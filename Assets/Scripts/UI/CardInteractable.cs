using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Guskapaska.UI
{
    /// <summary>
    /// Handles pointer hover and drag input for a single <see cref="CardView"/>.
    /// Manages visual feedback (hover lift, drag scale, layering) and exposes
    /// drag lifecycle events for higher-level controllers to react to.
    /// Drop detection itself is delegated to <c>DropZone</c> (Branch 2).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CardInteractable : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Hover Settings")]
        [SerializeField] private float hoverYOffset = 20f;
        [SerializeField] private Color hoverHighlightColor = new Color(1f, 0.9f, 0.3f, 1f);
        [SerializeField] private Outline hoverOutline;

        [Header("Drag Settings")]
        [SerializeField] private float dragScale = 1.2f;
        [SerializeField] private float dragRotationZ = 0f;

        [Header("Refs")]
        [SerializeField] private CardView cardView;
        [SerializeField] private CanvasGroup canvasGroup;

        /// <summary>Whether this card can currently be hovered or dragged.</summary>
        public bool Interactable { get; set; } = true;

        /// <summary>Raised when a drag operation begins on this card.</summary>
        public event Action<CardInteractable> OnDragStarted;

        /// <summary>
        /// Raised when a drag operation ends on this card.
        /// The bool argument indicates drop success — always false here;
        /// real success is determined by subscribers reacting to DropZone events.
        /// </summary>
        public event Action<CardInteractable, bool> OnDragEnded;

        /// <summary>The most recent pointer position observed during drag (screen coordinates).</summary>
        public Vector2 CurrentPointerPosition { get; private set; }

        /// <summary>The <see cref="CardView"/> this interactable wraps.</summary>
        public CardView CardView => cardView;

        // 드래그 시작 전 원본 transform 상태. 복귀 시 그대로 되돌림.
        private Vector3 _originalLocalPosition;
        private Quaternion _originalLocalRotation;
        private Vector3 _originalLocalScale;
        private Transform _originalParent;
        private int _originalSiblingIndex;

        // 호버 / 드래그 상태 플래그. 호버 처리는 드래그 중 무시되어야 함.
        private bool _isHovering;
        private bool _isDragging;

        private void Awake()
        {
            // Inspector에서 연결을 깜빡한 경우를 대비해 자동 할당.
            if (cardView == null)
            {
                cardView = GetComponent<CardView>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            // 스케일 원본은 프리팹 인스턴스 시점의 값을 보관. 호버는 위치만 건드리므로
            // 매 호버마다 갱신하지 않는다.
            _originalLocalScale = transform.localScale;

            // outline은 기본 비활성. 호버 시에만 켠다.
            if (hoverOutline != null)
            {
                hoverOutline.enabled = false;
            }
        }

        /// <inheritdoc/>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 드래그 중에는 호버 효과를 적용하지 않는다.
            if (!Interactable || _isDragging)
            {
                return;
            }

            // 호버 직전 위치를 기준점으로 저장. 드래그가 호버 도중 시작될 수도 있어
            // OnBeginDrag에서 이 값을 다시 참조한다.
            _originalLocalPosition = transform.localPosition;
            transform.localPosition = _originalLocalPosition + new Vector3(0f, hoverYOffset, 0f);

            if (hoverOutline != null)
            {
                hoverOutline.enabled = true;
                hoverOutline.effectColor = hoverHighlightColor;
            }

            _isHovering = true;
        }

        /// <inheritdoc/>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isHovering || _isDragging)
            {
                return;
            }

            // 호버로 올라간 만큼 다시 내려놓는다.
            transform.localPosition = _originalLocalPosition;

            if (hoverOutline != null)
            {
                hoverOutline.enabled = false;
            }

            _isHovering = false;
        }

        /// <inheritdoc/>
        public void OnBeginDrag(PointerEventData eventData)
        {
            // Unity는 IBeginDragHandler를 무조건 호출하므로 함수 안에서 가드.
            if (!Interactable)
            {
                return;
            }

            // 호버 중이었다면 호버 오프셋을 먼저 제거. 이렇게 해야
            // _originalLocalPosition이 호버 전 진짜 원본을 가리킨다.
            if (_isHovering)
            {
                transform.localPosition = _originalLocalPosition;

                if (hoverOutline != null)
                {
                    hoverOutline.enabled = false;
                }

                _isHovering = false;
            }

            // 복귀에 필요한 상태를 모두 저장.
            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();
            _originalLocalPosition = transform.localPosition;
            _originalLocalRotation = transform.localRotation;
            // _originalLocalScale은 Awake에서 잡아둔 프리팹 기본 스케일을 그대로 사용.

            // 부모를 Canvas 루트로 이동해 다른 카드 위에 그려지도록 한다.
            // rootCanvas를 쓰는 이유: 중첩 Canvas가 있어도 최상위에 붙기 위함.
            Canvas containingCanvas = GetComponentInParent<Canvas>();
            if (containingCanvas != null)
            {
                Transform canvasRoot = containingCanvas.rootCanvas.transform;
                // worldPositionStays=false: 부모 이동 직후 localPosition을 우리가 직접 덮어쓸 것이므로
                // Unity가 월드 위치 보존을 위해 localPosition을 임의로 바꾸지 않게 한다.
                transform.SetParent(canvasRoot, worldPositionStays: false);
                transform.SetAsLastSibling();
            }
            else
            {
                Debug.LogWarning($"[CardInteractable] {name} 의 부모 Canvas를 찾지 못했습니다. 드래그 레이어링이 깨질 수 있습니다.");
            }

            // 드래그 시각 효과 적용.
            transform.localScale = Vector3.one * dragScale;
            transform.localRotation = Quaternion.Euler(0f, 0f, dragRotationZ);

            // 드래그 중인 카드가 자기 아래 영역의 raycast를 막으면 DropZone이 OnDrop을 받지 못한다.
            // 02_Unity6_Guidelines.md의 Known Pitfalls 참조.
            canvasGroup.blocksRaycasts = false;

            _isDragging = true;
            OnDragStarted?.Invoke(this);
        }

        /// <inheritdoc/>
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            // Screen Space - Overlay Canvas에서는 스크린 좌표를 그대로 위치로 사용 가능.
            // Vector2 → Vector3는 z=0으로 암묵 변환.
            transform.position = eventData.position;
            CurrentPointerPosition = eventData.position;
        }

        /// <inheritdoc/>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            // raycast 차단을 먼저 풀어야 이후 호버/클릭이 정상 동작.
            canvasGroup.blocksRaycasts = true;
            _isDragging = false;

            // 성공/실패 판정은 구독자(DragController)의 책임.
            // 여기서는 일단 false로 발화하고, 실패 시 외부에서 ReturnToOrigin을 호출하도록 한다.
            OnDragEnded?.Invoke(this, false);
        }

        /// <summary>
        /// Restores this card to the parent, sibling order, position, rotation, and scale
        /// captured at the start of the most recent drag.
        /// Instant (no tween) — Stage 5 will replace this with a smooth coroutine.
        /// </summary>
        public void ReturnToOrigin()
        {
            if (_originalParent == null)
            {
                // 드래그를 한 번도 시작하지 않은 경우. 호출당해도 무해하게 처리.
                return;
            }

            transform.SetParent(_originalParent, worldPositionStays: false);
            transform.SetSiblingIndex(_originalSiblingIndex);
            transform.localPosition = _originalLocalPosition;
            transform.localRotation = _originalLocalRotation;
            transform.localScale = _originalLocalScale;
        }

        /// <summary>
        /// Forces the card out of the hovered state without waiting for OnPointerExit.
        /// Useful when external logic needs to ensure the card is at its rest position
        /// (e.g. when a new round starts and hands are re-rendered).
        /// </summary>
        public void ForceUnhover()
        {
            if (!_isHovering)
            {
                return;
            }

            transform.localPosition = _originalLocalPosition;

            if (hoverOutline != null)
            {
                hoverOutline.enabled = false;
            }

            _isHovering = false;
        }
    }
}
