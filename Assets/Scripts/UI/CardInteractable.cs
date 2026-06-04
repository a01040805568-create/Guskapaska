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

        public bool Interactable { get; set; } = true;

        public event Action<CardInteractable> OnDragStarted;
        public event Action<CardInteractable, bool> OnDragEnded;

        public Vector2 CurrentPointerPosition { get; private set; }
        public CardView CardView => cardView;
        public bool IsDragging => _isDragging;

        private Vector3 _restLocalPosition;
        private Quaternion _restLocalRotation;
        private Vector3 _restLocalScale;
        private Transform _restParent;
        private int _restSiblingIndex;

        private bool _isHovering;
        private bool _isDragging;
        private bool _restCaptured;

        private string HoverKey => $"hover_{GetInstanceID()}";
        private string ScaleKey => $"scale_{GetInstanceID()}";
        private string ReturnKey => $"return_{GetInstanceID()}";
        private string ReturnRotKey => $"returnRot_{GetInstanceID()}";

        private void Awake()
        {
            if (cardView == null) cardView = GetComponent<CardView>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            if (hoverOutline != null)
            {
                hoverOutline.enabled = false;
            }
        }

        private void OnEnable()
        {
            CaptureRestState();
        }

        private void OnDisable()
        {
            TweenRunner.CancelAll(this);

            _isHovering = false;
            _isDragging = false;

            if (hoverOutline != null) hoverOutline.enabled = false;
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// Forces hover and drag state flags to false and stops any in-flight tween.
        /// Called by HandView before applying a new layout so that the next CaptureRestState
        /// always succeeds, even if this CardView instance was being dragged on the previous round.
        /// </summary>
        public void ResetInteractionState()
        {
            TweenRunner.CancelAll(this);

            _isHovering = false;
            _isDragging = false;

            if (hoverOutline != null) hoverOutline.enabled = false;
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// Captures the current transform as the "rest" state — the position this card
        /// should return to after any hover or drag. HandView calls this after applying
        /// the layout for each card, so the rest position always tracks the layout.
        /// </summary>
        public void CaptureRestState()
        {
            // 이전 가드 (_isHovering || _isDragging) 는 외부 코드가 명시적으로 rest를 갱신하려는
            // 경우를 차단하는 부작용이 있어, 가드를 제거하고 항상 현재 transform을 rest로 잡는다.
            // 호버/드래그 도중 외부에서 CaptureRestState를 부르는 경우는 거의 없으며,
            // 있다 해도 HandView가 ResetInteractionState로 먼저 정리하므로 안전하다.
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!Interactable || _isDragging) return;

            if (!_restCaptured) CaptureRestState();

            // 호버는 카드의 회전(angleZ)을 무시하고 단순히 위쪽으로 띄운다.
            // 부채꼴에서 양 끝 카드는 회전되어 있지만, 호버 시 살짝 떠오르는 것은
            // 카드 기준 위쪽이 아니라 화면 기준 위쪽 (Y+).
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

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isHovering || _isDragging) return;

            TweenRunner.Run(this, HoverKey,
                TweenRunner.MoveLocal(transform, transform.localPosition, _restLocalPosition, hoverDuration, EasingCurves.EaseOutQuad));

            if (hoverOutline != null) hoverOutline.enabled = false;

            _isHovering = false;
        }

        // ─────────────────────────────────────────────────────────────
        // 드래그 시작 / 진행 / 종료
        // ─────────────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!Interactable) return;

            if (!_restCaptured) CaptureRestState();

            TweenRunner.Cancel(this, HoverKey);

            if (_isHovering)
            {
                if (hoverOutline != null) hoverOutline.enabled = false;
                _isHovering = false;
            }

            TweenRunner.Cancel(this, ReturnKey);
            TweenRunner.Cancel(this, ReturnRotKey);

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

            // 드래그 중에는 회전을 0으로 맞춘다 (부채꼴에서 기울어진 카드도 드래그할 때는 똑바로).
            transform.localRotation = Quaternion.Euler(0f, 0f, dragRotationZ);

            Vector3 targetScale = Vector3.one * dragScale;
            TweenRunner.Run(this, ScaleKey,
                TweenRunner.Scale(transform, transform.localScale, targetScale, dragScaleDuration, EasingCurves.EaseOutQuad));

            canvasGroup.blocksRaycasts = false;
            _isDragging = true;
            OnDragStarted?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            transform.position = eventData.position;
            CurrentPointerPosition = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            canvasGroup.blocksRaycasts = true;
            _isDragging = false;

            OnDragEnded?.Invoke(this, false);
        }

        // ─────────────────────────────────────────────────────────────
        // 복귀
        // ─────────────────────────────────────────────────────────────

        public void ReturnToOrigin()
        {
            if (!_restCaptured || _restParent == null) return;

            transform.SetParent(_restParent, worldPositionStays: false);
            transform.SetSiblingIndex(_restSiblingIndex);

            Vector3 fromPos = transform.localPosition;
            Quaternion fromRot = transform.localRotation;
            Vector3 fromScale = transform.localScale;

            TweenRunner.Run(this, ReturnKey,
                TweenRunner.MoveLocal(transform, fromPos, _restLocalPosition, returnDuration, EasingCurves.EaseOutBack));

            TweenRunner.Run(this, ReturnRotKey,
                TweenRunner.Rotate(transform, fromRot, _restLocalRotation, returnDuration, EasingCurves.EaseOutQuad));

            TweenRunner.Run(this, ScaleKey,
                TweenRunner.Scale(transform, fromScale, _restLocalScale, returnDuration, EasingCurves.EaseOutQuad));

            _isHovering = false;
            if (hoverOutline != null) hoverOutline.enabled = false;
        }

        public void ReturnToOriginInstant()
        {
            TweenRunner.Cancel(this, ReturnKey);
            TweenRunner.Cancel(this, ReturnRotKey);
            TweenRunner.Cancel(this, ScaleKey);
            TweenRunner.Cancel(this, HoverKey);

            if (!_restCaptured || _restParent == null) return;

            transform.SetParent(_restParent, worldPositionStays: false);
            transform.SetSiblingIndex(_restSiblingIndex);
            transform.localPosition = _restLocalPosition;
            transform.localRotation = _restLocalRotation;
            transform.localScale = _restLocalScale;

            _isHovering = false;
            _isDragging = false;

            if (hoverOutline != null) hoverOutline.enabled = false;
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        }

        public void ForceUnhover()
        {
            if (!_isHovering) return;

            TweenRunner.Cancel(this, HoverKey);
            if (_restCaptured)
            {
                transform.localPosition = _restLocalPosition;
            }

            if (hoverOutline != null) hoverOutline.enabled = false;
            _isHovering = false;
        }
    }
}
