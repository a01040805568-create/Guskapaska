using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Guskapaska.Core;
using Guskapaska.Util;

namespace Guskapaska.UI
{
    /// <summary>
    /// Renders a hand of cards as a fan that always spreads to fit the current
    /// number of cards. The spread uses a per-card angle increment so the visual
    /// gap between adjacent cards stays consistent regardless of hand size.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private GameObject cardViewPrefab;
        [SerializeField] private bool faceUp = true;

        [Header("Interaction")]
        [Tooltip("이 손패의 카드들이 호버/드래그 입력을 받는지 여부.")]
        [SerializeField] private bool interactable = true;

        [Header("Fan Layout")]
        [SerializeField] private bool useFanLayout = true;
        [SerializeField] private float cardSpacing = 80f;

        [Tooltip("부채꼴 호의 가상 반지름(픽셀). 작을수록 강하게 휜다. 권장 700~1200.")]
        [SerializeField] private float fanRadius = 1000f;

        [Tooltip("카드 사이의 각도 간격(도). 카드 수에 비례해서 전체 부채꼴 폭이 변한다. 권장 6~10도.")]
        [SerializeField] private float anglePerCard = 7f;

        [Tooltip("부채꼴이 펼쳐지는 방향. true면 손패 아래에서 위로(플레이어), false면 위에서 아래(AI).")]
        [SerializeField] private bool fanOpensUpward = true;

        // 레거시 호환.
        [SerializeField] private float arcAngleDegrees = 0f;
        [SerializeField] private float arcHeight = 0f;

        [Header("Deal Animation")]
        [SerializeField] private float dealDurationPerCard = 0.4f;
        [SerializeField] private float dealStagger = 0.08f;
        [SerializeField] private float dealStartOffsetX = -800f;
        [SerializeField] private float dealStartOffsetY = 0f;

        private readonly List<CardView> _activeViews = new List<CardView>();

        // 진행 중인 딜 코루틴 핸들. 새 Render 호출 시 강제 중단.
        private readonly List<Coroutine> _dealCoroutines = new List<Coroutine>();

        public IReadOnlyList<CardView> ActiveViews => _activeViews;
        public bool Interactable => interactable;

        private void OnDisable()
        {
            CancelAllDealCoroutines();
            TweenRunner.CancelAll(this);
        }

        public void Render(IReadOnlyList<Card> cards)
        {
            if (cards == null)
            {
                Clear();
                return;
            }

            // 진행 중인 딜 코루틴이 있으면 강제 중단 — 그래야 새 Render가 위치를 확정할 수 있다.
            CancelAllDealCoroutines();

            EnsureViewCount(cards.Count);

            int total = cards.Count;

            // 풀 전체 일괄 정리.
            for (int i = 0; i < _activeViews.Count; i++)
            {
                CardView view = _activeViews[i];
                if (view == null) continue;

                CardInteractable ci = view.GetComponent<CardInteractable>();
                if (ci != null) ci.ResetInteractionState();

                ReclaimToContainer(view);
                view.transform.SetSiblingIndex(i);
            }

            // 사용할 카드 배치.
            for (int i = 0; i < total; i++)
            {
                CardView view = _activeViews[i];

                view.gameObject.SetActive(true);
                view.Bind(cards[i]);
                view.SetFaceUp(faceUp);
                ApplyInteractableToView(view);

                ApplyLayoutAt(view, i, total);

                CardInteractable ci = view.GetComponent<CardInteractable>();
                if (ci != null) ci.CaptureRestState();
            }

            // 남는 카드 비활성화.
            for (int i = total; i < _activeViews.Count; i++)
            {
                CardView view = _activeViews[i];
                if (view == null) continue;

                RectTransform rt = view.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.zero;
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                }

                view.Clear();
                view.gameObject.SetActive(false);
            }
        }

        public IEnumerator RenderWithDealAnimation(IReadOnlyList<Card> cards)
        {
            if (cards == null)
            {
                Clear();
                yield break;
            }

            // 이전 딜이 진행 중이면 모두 중단 (안전망).
            CancelAllDealCoroutines();

            EnsureViewCount(cards.Count);

            int total = cards.Count;
            float totalDuration = (total - 1) * dealStagger + dealDurationPerCard;

            for (int i = 0; i < _activeViews.Count; i++)
            {
                CardView view = _activeViews[i];
                if (view == null) continue;

                CardInteractable ci = view.GetComponent<CardInteractable>();
                if (ci != null) ci.ResetInteractionState();

                ReclaimToContainer(view);
                view.transform.SetSiblingIndex(i);
            }

            for (int i = 0; i < total; i++)
            {
                CardView view = _activeViews[i];
                view.gameObject.SetActive(true);
                view.Bind(cards[i]);
                view.SetFaceUp(faceUp);
                ApplyInteractableToView(view);

                Vector3 finalPos;
                float finalAngle;
                ComputeLayoutAt(i, total, out finalPos, out finalAngle);

                Vector3 startPos = finalPos + new Vector3(dealStartOffsetX, dealStartOffsetY, 0f);

                RectTransform rt = view.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(startPos.x, startPos.y);
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;

                // 딜 코루틴 핸들을 보관하여 필요 시 중단할 수 있게 한다.
                Coroutine c = StartCoroutine(DealOneCard(view, rt, startPos, finalPos, finalAngle, i * dealStagger));
                _dealCoroutines.Add(c);
            }

            for (int i = total; i < _activeViews.Count; i++)
            {
                CardView view = _activeViews[i];
                if (view == null) continue;

                RectTransform rt = view.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.zero;
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                }
                view.Clear();
                view.gameObject.SetActive(false);
            }

            // 모든 딜 코루틴 종료 대기.
            yield return new WaitForSeconds(totalDuration);

            // 딜이 끝났으니 핸들 리스트 비움.
            _dealCoroutines.Clear();

            // 핵심: 딜 종료 후 모든 카드의 최종 위치를 한 번 더 강제 적용.
            // 부동소수점 오차나 도중 중단 등으로 어긋났을 가능성을 차단한다.
            for (int i = 0; i < total; i++)
            {
                CardView view = _activeViews[i];
                if (view == null) continue;

                ApplyLayoutAt(view, i, total);

                CardInteractable ci = view.GetComponent<CardInteractable>();
                if (ci != null) ci.CaptureRestState();
            }
        }

        public void Clear()
        {
            CancelAllDealCoroutines();

            foreach (CardView view in _activeViews)
            {
                if (view == null) continue;

                CardInteractable ci = view.GetComponent<CardInteractable>();
                if (ci != null) ci.ResetInteractionState();

                ReclaimToContainer(view);

                RectTransform rt = view.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.zero;
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                }

                view.Clear();
                view.gameObject.SetActive(false);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 레이아웃 계산
        // ─────────────────────────────────────────────────────────────

        private void ComputeLayoutAt(int index, int total, out Vector3 pos, out float angleZ)
        {
            if (useFanLayout && total > 0)
            {
                ComputeFanLayoutAt(index, total, out pos, out angleZ);
            }
            else
            {
                ComputeLinearLayoutAt(index, total, out pos, out angleZ);
            }
        }

        private void ComputeFanLayoutAt(int index, int total, out Vector3 pos, out float angleZ)
        {
            if (total == 1)
            {
                pos = Vector3.zero;
                angleZ = 0f;
                return;
            }

            float centerY = fanOpensUpward ? -fanRadius : fanRadius;

            // 카드 사이 각도를 anglePerCard로 고정. 전체 각도 = anglePerCard * (total - 1).
            float totalSpread = anglePerCard * (total - 1);
            float halfSpread = totalSpread * 0.5f;

            float angleFromCenter = -halfSpread + index * anglePerCard;

            float visualAngleZ = fanOpensUpward ? -angleFromCenter : angleFromCenter;

            float rad = angleFromCenter * Mathf.Deg2Rad;
            float x = Mathf.Sin(rad) * fanRadius;
            float yOffset = Mathf.Cos(rad) * fanRadius;

            float y = yOffset + centerY;

            pos = new Vector3(x, y, 0f);
            angleZ = visualAngleZ;
        }

        private void ComputeLinearLayoutAt(int index, int total, out Vector3 pos, out float angleZ)
        {
            float startX = -((total - 1) * cardSpacing) * 0.5f;
            float x = startX + index * cardSpacing;
            float y = 0f;
            float angle = 0f;

            if (Mathf.Abs(arcAngleDegrees) > 0.0001f && total > 1)
            {
                float t = (index / (float)(total - 1)) - 0.5f;
                angle = -t * arcAngleDegrees;
                y = -Mathf.Abs(t) * arcHeight;
            }

            pos = new Vector3(x, y, 0f);
            angleZ = angle;
        }

        private void ApplyLayoutAt(CardView view, int index, int total)
        {
            Vector3 pos;
            float angleZ;
            ComputeLayoutAt(index, total, out pos, out angleZ);

            RectTransform rt = view.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(pos.x, pos.y);
            rt.localRotation = Quaternion.Euler(0f, 0f, angleZ);
            rt.localScale = Vector3.one;
        }

        // ─────────────────────────────────────────────────────────────
        // 내부 유틸
        // ─────────────────────────────────────────────────────────────

        // 진행 중인 모든 딜 코루틴을 중단.
        private void CancelAllDealCoroutines()
        {
            for (int i = 0; i < _dealCoroutines.Count; i++)
            {
                if (_dealCoroutines[i] != null)
                {
                    StopCoroutine(_dealCoroutines[i]);
                }
            }
            _dealCoroutines.Clear();
        }

        private void EnsureViewCount(int needed)
        {
            while (_activeViews.Count < needed)
            {
                GameObject go = Instantiate(cardViewPrefab, cardContainer);
                CardView view = go.GetComponent<CardView>();
                _activeViews.Add(view);
                ApplyInteractableToView(view);
            }
        }

        private IEnumerator DealOneCard(CardView view, RectTransform rt, Vector3 startPos, Vector3 finalPos, float finalAngle, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (view == null || rt == null) yield break;

            float elapsed = 0f;
            AnimationCurve curve = EasingCurves.EaseOutQuad;
            float startAngle = 0f;

            while (elapsed < dealDurationPerCard)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / dealDurationPerCard);
                float k = curve.Evaluate(u);

                Vector3 pos = Vector3.LerpUnclamped(startPos, finalPos, k);
                rt.anchoredPosition = new Vector2(pos.x, pos.y);

                float angle = Mathf.LerpUnclamped(startAngle, finalAngle, k);
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);

                if (rt == null) yield break;
                yield return null;
            }

            rt.anchoredPosition = new Vector2(finalPos.x, finalPos.y);
            rt.localRotation = Quaternion.Euler(0f, 0f, finalAngle);
        }

        private void ReclaimToContainer(CardView view)
        {
            if (view == null || cardContainer == null) return;

            if (view.transform.parent != cardContainer)
            {
                view.transform.SetParent(cardContainer, worldPositionStays: false);
            }

            CanvasGroup cg = view.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = true;
                cg.alpha = 1f;
            }
        }

        private void ApplyInteractableToView(CardView view)
        {
            if (view == null) return;

            CardInteractable ci = view.GetComponent<CardInteractable>();
            if (ci == null) return;

            ci.Interactable = interactable;
            ci.enabled = interactable;
        }
    }
}
