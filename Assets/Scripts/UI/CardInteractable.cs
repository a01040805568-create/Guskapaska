using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Guskapaska.Util;

namespace Guskapaska.UI
{
    /// <summary>
    /// Handles pointer hover and drag input for a single <see cref="CardView"/>.
    /// Manages visual feedback (hover lift, drag scale, layering) and exposes
    /// drag lifecycle events for higher-level controllers to react to.
    /// Drop detection itself is delegated to <c>DropZone</c>.
    /// Stage 5 replaces the instant transform changes with smooth tweens.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CardInteractable : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Hover Settings")]
        [SerializeField] private float hoverYOffset = 20f;
        [SerializeField] private float hoverDuration = 0.15f;
        [SerializeField] private Color hoverHighlightColor = new Color(1f, 0.9f, 0.3f, 1f);
        [SerializeField] private Outline hoverOutline;

        [Header("Drag Settings")]
        [SerializeField] private float dragScale = 1.2f;
        [SerializeField] private float dragRotationZ = 0f;
        [SerializeField] private float dragScaleDuration = 0.1f;

        [Header("Return Settings")]
        [SerializeField] private float returnDuration = 0.25f;

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

        /// <summary>Whether this card is currently being dragged. Read-only.</summary>
        public bool IsDragging => _isDragging;

        // 드래그 시작 전 원본 transform 상태. 복귀 시 그대로 되돌림.
        // _restLocalPosition: 호버/드래그가 전혀 없을 때의 진짜 원위치.
        //                     HandView.Render가 설정한 마지막 위치이며,
        //                     호버나 드래그 시작 시 이 값을 갱신하지 않는다 (핵심!).
        private Vector3 _restLocalPosition;
        private Quaternion _restLocalRotation;
        private Vector3 _restLocalScale;
        private Transform _restParent;
        private int _restSiblingIndex;

        // 호버 / 드래그 상태 플래그. 호버 처리는 드래그 중 무시되어야 함.
        private bool _isHovering;
        private bool _isDragging;

        // rest 상태가 한 번이라도 캡처됐는지. 첫 호버/드래그 전에는 false.
        private bool _restCaptured;

        // 트윈 키 — TweenRunner에서 동일 카드의 진행 중 트윈을 식별하는 ID.
        private string HoverKey  => $"hover_{GetInstanceID()}";
        private string ScaleKey  => $"scale_{GetInstanceID()}";
        private string ReturnKey => $"return_{GetInstanceID()}";
        private string ReturnRotKey => $"returnRot_{GetInstanceID()}";

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

            // outline은 기본 비활성. 호버 시에만 켠다.
            if (hoverOutline != null)
            {
                hoverOutline.enabled = false;
            }
        }

        private void OnEnable()
        {
            // 카드가 (재)활성화될 때 현재 transform 상태를 rest로 캡처.
            // HandView.Render → SetActive(true) → 위치 설정 순서일 가능성이 있어
            // OnEnable 시점의 값이 안 맞을 수 있지만, 첫 호버 시점에 강제로 다시 캡처하므로 안전.
            CaptureRestState();
        }

        private void OnDisable()
        {
            // 컴포넌트 비활성화 시 진행 중인 모든 트윈 취소.
            TweenRunner.CancelAll(this);

            // 호버/드래그 상태 강제 초기화. 재활성화 시 깨끗한 상태로 시작.
            _isHovering = false;
            _isDragging = false;

            if (hoverOutline != null)
            {
                hoverOutline.enabled = false;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// Captures the current transform as the "rest" state — the position the card
        /// should return to after any hover or drag operation. Called by HandView after
        /// laying out cards, and by OnEnable as a safety net.
        /// </summary>
        public void CaptureRestState()
        {
            // 호버나 드래그 중인 카드의 rest를 덮어쓰지 않는다 (현재 transform이 흐트러진 상태).
            // 단, _restCaptured가 false이면 어떤 상태든 강제로 캡처 (첫 호출).
            if (_restCaptured && (_isHovering || _isDragging))
            {
                return;
            }

            _restLocalPosition = transform.localPosition;
            _restLocalRotation = transform.localRotation;
            _restLocalScale = transform.localScale;
            _restParent = transform.parent;
            _restSiblingIndex = transform.GetSiblingIndex();
            _restCaptured = true;
        }

        // ─────────────────────────────────────────────────────────────
        // 호버
        // ─────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 드래그 중에는 호버 효과를 적용하지 않는다.
            if (!Interactable || _isDragging)
            {
                return;
            }

            // 호버 시작 전에 현재 위치가 rest와 일치하는지 점검.
            // 만약 외부 코드(HandView.Render)가 위치를 바꿨다면 새 위치를 rest로 채택.
            // 단, 이전 호버의 트윈 도중 위치가 _restLocalPosition + offset 근처일 수 있으므로
            // 트윈이 진행 중인지 여부도 함께 본다.
            if (!_restCaptured)
            {
                CaptureRestState();
            }

            // 호버 위치 = rest + 위쪽 오프셋. rest 자체는 절대 건드리지 않는다.
            Vector3 hoverTarget = _restLocalPosition + new Vector3(0f, hoverYOffset, 0f);
            TweenRunner.Run(this, HoverKey,
                TweenRunner.MoveLocal(transform, transform.localPosition, hoverTarget, hoverDuration, EasingCurves.EaseOutQuad));

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

            // rest 위치로 부드럽게 복귀.
            TweenRunner.Run(this, HoverKey,
                TweenRunner.MoveLocal(transform, transform.localPosition, _restLocalPosition, hoverDuration, EasingCurves.EaseOutQuad));

            if (hoverOutline != null)
            {
                hoverOutline.enabled = false;
            }

            _isHovering = false;
        }

        // ─────────────────────────────────────────────────────────────
        // 드래그 시작 / 진행 / 종료
        // ─────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void OnBeginDrag(PointerEventData eventData)
        {
            // Unity는 IBeginDragHandler를 무조건 호출하므로 함수 안에서 가드.
            if (!Interactable)
            {
                return;
            }

            // rest 상태가 캡처되지 않았다면 지금 캡처 (안전망).
            if (!_restCaptured)
            {
                CaptureRestState();
            }

            // 호버 트윈이 진행 중일 수 있으므로 즉시 취소.
            TweenRunner.Cancel(this, HoverKey);

            // 호버 중이었다면 호버 오프셋을 먼저 제거.
            // 핵심: rest 값은 절대 갱신하지 않는다. 호버는 일시적 시각 상태일 뿐.
            if (_isHovering)
            {
                if (hoverOutline != null)
                {
                    hoverOutline.enabled = false;
                }
                _isHovering = false;
            }

            // 진행 중인 복귀 트윈도 취소.
            TweenRunner.Cancel(this, ReturnKey);
            TweenRunner.Cancel(this, ReturnRotKey);

            // 부모를 Canvas 루트로 이동해 다른 카드 위에 그려지도록 한다.
            Canvas containingCanvas = GetComponentInParent<Canvas>();
            if (containingCanvas != null)
            {
                Transform canvasRoot = containingCanvas.rootCanvas.transform;
                transform.SetParent(canvasRoot, worldPositionStays: true);
                transform.SetAsLastSibling();
            }
            else
            {
                Debug.LogWarning($"[CardInteractable] {name} 의 부모 Canvas를 찾지 못했습니다.");
            }

            // 회전은 즉시 적용.
            transform.localRotation = Quaternion.Euler(0f, 0f, dragRotationZ);

            // 스케일은 트윈으로 부드럽게 확대.
            Vector3 targetScale = Vector3.one * dragScale;
            TweenRunner.Run(this, ScaleKey,
                TweenRunner.Scale(transform, transform.localScale, targetScale, dragScaleDuration, EasingCurves.EaseOutQuad));

            // 드래그 중인 카드가 자기 아래 영역의 raycast를 막으면 DropZone이 OnDrop을 받지 못한다.
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

            // 마우스 추적은 즉시 (트윈 없음).
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

            canvasGroup.blocksRaycasts = true;
            _isDragging = false;

            // OnDragEnded는 즉시 발화. 실제 복귀/제출 판정은 구독자(DragController) 책임.
            OnDragEnded?.Invoke(this, false);
        }

        // ─────────────────────────────────────────────────────────────
        // 복귀
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Restores this card to its rest state (the position it had before any
        /// hover or drag started) with a smooth tween.
        /// </summary>
        public void ReturnToOrigin()
        {
            if (!_restCaptured || _restParent == null)
            {
                // rest 상태가 캡처되지 않은 경우 (드래그 한 번도 안 한 카드). 무해 처리.
                return;
            }

            // 부모와 형제 순서는 즉시 복원.
            transform.SetParent(_restParent, worldPositionStays: false);
            transform.SetSiblingIndex(_restSiblingIndex);

            // 부모 변경 직후의 현재 transform 값을 시작점으로 사용.
            Vector3 fromPos = transform.localPosition;
            Quaternion fromRot = transform.localRotation;
            Vector3 fromScale = transform.localScale;

            // 위치 / 회전 / 스케일 동시 트윈.
            // EaseOutBack은 약간의 오버슈트로 톡 튀는 마무리감.
            TweenRunner.Run(this, ReturnKey,
                TweenRunner.MoveLocal(transform, fromPos, _restLocalPosition, returnDuration, EasingCurves.EaseOutBack));

            TweenRunner.Run(this, ReturnRotKey,
                TweenRunner.Rotate(transform, fromRot, _restLocalRotation, returnDuration, EasingCurves.EaseOutQuad));

            TweenRunner.Run(this, ScaleKey,
                TweenRunner.Scale(transform, fromScale, _restLocalScale, returnDuration, EasingCurves.EaseOutQuad));

            // 호버 상태는 명시적으로 해제. ReturnToOrigin은 "원래 자리로 완전 복귀"이므로
            // 호버 잔재가 남으면 안 된다.
            _isHovering = false;
            if (hoverOutline != null)
            {
                hoverOutline.enabled = false;
            }
        }

        /// <summary>
        /// Restores this card to its rest state immediately, cancelling any in-flight tweens.
        /// Use this for emergency cleanup (match end, forced reset).
        /// </summary>
        public void ReturnToOriginInstant()
        {
            TweenRunner.Cancel(this, ReturnKey);
            TweenRunner.Cancel(this, ReturnRotKey);
            TweenRunner.Cancel(this, ScaleKey);
            TweenRunner.Cancel(this, HoverKey);

            if (!_restCaptured || _restParent == null)
            {
                return;
            }

            transform.SetParent(_restParent, worldPositionStays: false);
            transform.SetSiblingIndex(_restSiblingIndex);
            transform.localPosition = _restLocalPosition;
            transform.localRotation = _restLocalRotation;
            transform.localScale = _restLocalScale;

            _isHovering = false;
            _isDragging = false;

            if (hoverOutline != null)
            {
                hoverOutline.enabled = false;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// Forces the card out of the hovered state without waiting for OnPointerExit.
        /// </summary>
        public void ForceUnhover()
        {
            if (!_isHovering)
            {
                return;
            }

            TweenRunner.Cancel(this, HoverKey);
            if (_restCaptured)
            {
                transform.localPosition = _restLocalPosition;
            }

            if (hoverOutline != null)
            {
                hoverOutline.enabled = false;
            }

            _isHovering = false;
        }
    }
}