using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Guskapaska.Core;
using Guskapaska.Util;

namespace Guskapaska.UI
{
    /// <summary>
    /// Renders a row of cards for either the player or the AI hand.
    /// Stage 3 uses a straight-line layout; the fan layout is added in Stage 5 Branch 5.
    /// Stage 5 Branch 3 adds a deal animation that slides cards in from outside the screen
    /// to their layout position with a stagger.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private GameObject cardViewPrefab;
        [SerializeField] private bool faceUp = true;

        [Header("Interaction")]
        [Tooltip("이 손패의 카드들이 호버/드래그 입력을 받는지 여부. 플레이어 핸드는 true, AI 핸드는 false.")]
        [SerializeField] private bool interactable = true;

        [Header("Fan Layout")]
        [SerializeField] private float cardSpacing = 80f;
        [SerializeField] private float arcAngleDegrees = 0f;
        [SerializeField] private float arcHeight = 0f;

        [Header("Deal Animation")]
        [Tooltip("매치 시작 시 카드가 화면 밖에서 손패로 슬라이드 인 하는 지속 시간(초).")]
        [SerializeField] private float dealDurationPerCard = 0.4f;

        [Tooltip("카드 사이의 딜 시작 간격(초). 카드들이 순차적으로 등장하도록 함.")]
        [SerializeField] private float dealStagger = 0.08f;

        [Tooltip("딜 시작 시 카드가 위치하는 시작점의 X 오프셋(픽셀). 음수면 왼쪽 밖에서 시작.")]
        [SerializeField] private float dealStartOffsetX = -800f;

        [Tooltip("딜 시작 시 카드가 위치하는 시작점의 Y 오프셋(픽셀). 0이면 손패와 같은 높이에서 시작.")]
        [SerializeField] private float dealStartOffsetY = 0f;

        private readonly List<CardView> _activeViews = new List<CardView>();

        public IReadOnlyList<CardView> ActiveViews => _activeViews;
        public bool Interactable => interactable;

        private void OnDisable()
        {
            // 진행 중인 딜 트윈 정리.
            TweenRunner.CancelAll(this);
        }

        /// <summary>
        /// Render the given cards immediately at their final layout positions.
        /// Used for incremental updates within a round.
        /// </summary>
        public void Render(IReadOnlyList<Card> cards)
        {
            if (cards == null)
            {
                Clear();
                return;
            }

            EnsureViewCount(cards.Count);

            int total = cards.Count;
            float startX = -((total - 1) * cardSpacing) * 0.5f;

            for (int i = 0; i < total; i++)
            {
                CardView view = _activeViews[i];
                ReclaimToContainer(view);

                view.gameObject.SetActive(true);
                view.Bind(cards[i]);
                view.SetFaceUp(faceUp);
                ApplyInteractableToView(view);

                ApplyLayoutAt(view, i, total, startX);

                // rest 상태 캡처 — 호버/드래그/복귀의 기준점.
                CardInteractable ci = view.GetComponent<CardInteractable>();
                if (ci != null)
                {
                    ci.CaptureRestState();
                }
            }
        }

        /// <summary>
        /// Render the given cards with a deal-in animation: each card starts from an
        /// offscreen offset and slides to its layout position, with a stagger between cards.
        /// Used at match start.
        /// </summary>
        public IEnumerator RenderWithDealAnimation(IReadOnlyList<Card> cards)
        {
            if (cards == null)
            {
                Clear();
                yield break;
            }

            EnsureViewCount(cards.Count);

            int total = cards.Count;
            float startX = -((total - 1) * cardSpacing) * 0.5f;

            // 카드별로 트윈을 동시에 시작하되, 시작 시점만 stagger로 어긋나게 한다.
            // 마지막 카드의 트윈이 끝날 때까지 기다린 후 코루틴을 종료한다.
            float totalDuration = (total - 1) * dealStagger + dealDurationPerCard;

            for (int i = 0; i < total; i++)
            {
                CardView view = _activeViews[i];
                ReclaimToContainer(view);

                view.gameObject.SetActive(true);
                view.Bind(cards[i]);
                view.SetFaceUp(faceUp);
                ApplyInteractableToView(view);

                // 최종 레이아웃 위치 계산.
                RectTransform rt = view.GetComponent<RectTransform>();
                float finalX = startX + i * cardSpacing;
                float finalY = 0f;
                float finalAngle = 0f;

                if (Mathf.Abs(arcAngleDegrees) > 0.0001f && total > 1)
                {
                    float t = (i / (float)(total - 1)) - 0.5f;
                    finalAngle = -t * arcAngleDegrees;
                    finalY = -Mathf.Abs(t) * arcHeight;
                }

                Vector3 finalPos = new Vector3(finalX, finalY, 0f);
                Vector3 startPos = finalPos + new Vector3(dealStartOffsetX, dealStartOffsetY, 0f);

                // 시작 위치/회전/스케일로 즉시 배치.
                rt.anchoredPosition = new Vector2(startPos.x, startPos.y);
                rt.localRotation = Quaternion.Euler(0f, 0f, finalAngle);
                rt.localScale = Vector3.one;

                // 카드 i에 대해 stagger만큼 지연 후 트윈 시작.
                // 별도 코루틴으로 띄워야 다음 카드의 stagger 처리가 진행된다.
                StartCoroutine(DealOneCard(view, rt, startPos, finalPos, i * dealStagger));
            }

            // 모든 카드의 트윈이 끝날 때까지 대기.
            // 마지막 카드 = stagger * (total-1) + dealDurationPerCard 시간 후 종료.
            yield return new WaitForSeconds(totalDuration);

            // 모든 카드의 rest 상태를 최종 위치 기준으로 캡처.
            // (트윈 도중에는 위치가 흐트러져 있어 캡처하면 잘못된 rest가 잡힌다.)
            for (int i = 0; i < total; i++)
            {
                CardView view = _activeViews[i];
                if (view == null) continue;

                CardInteractable ci = view.GetComponent<CardInteractable>();
                if (ci != null)
                {
                    ci.CaptureRestState();
                }
            }
        }

        /// <summary>
        /// Hide all active CardViews without destroying them.
        /// </summary>
        public void Clear()
        {
            foreach (CardView view in _activeViews)
            {
                ReclaimToContainer(view);
                view.Clear();
                view.gameObject.SetActive(false);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 내부 유틸
        // ─────────────────────────────────────────────────────────────

        // 인스턴스 풀 크기 확장 및 남는 뷰 비활성화.
        private void EnsureViewCount(int needed)
        {
            while (_activeViews.Count < needed)
            {
                GameObject go = Instantiate(cardViewPrefab, cardContainer);
                CardView view = go.GetComponent<CardView>();
                _activeViews.Add(view);
                ApplyInteractableToView(view);
            }

            for (int i = needed; i < _activeViews.Count; i++)
            {
                ReclaimToContainer(_activeViews[i]);
                _activeViews[i].gameObject.SetActive(false);
            }
        }

        // 카드 한 장을 최종 위치로 슬라이드 인.
        private IEnumerator DealOneCard(CardView view, RectTransform rt, Vector3 startPos, Vector3 finalPos, float delay)
        {
            // stagger 지연.
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            // 트윈 도중 view가 파괴되거나 비활성화되면 안전하게 종료.
            if (view == null || rt == null)
            {
                yield break;
            }

            float elapsed = 0f;
            AnimationCurve curve = EasingCurves.EaseOutQuad;

            while (elapsed < dealDurationPerCard)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / dealDurationPerCard);
                float k = curve.Evaluate(u);

                Vector3 pos = Vector3.LerpUnclamped(startPos, finalPos, k);
                rt.anchoredPosition = new Vector2(pos.x, pos.y);

                if (rt == null) yield break;
                yield return null;
            }

            // 정확한 최종 위치 보장.
            rt.anchoredPosition = new Vector2(finalPos.x, finalPos.y);
        }

        // 카드 i를 즉시 레이아웃 위치에 배치.
        private void ApplyLayoutAt(CardView view, int index, int total, float startX)
        {
            RectTransform rt = view.GetComponent<RectTransform>();
            float x = startX + index * cardSpacing;
            float y = 0f;
            float angle = 0f;

            if (Mathf.Abs(arcAngleDegrees) > 0.0001f && total > 1)
            {
                float t = (index / (float)(total - 1)) - 0.5f;
                angle = -t * arcAngleDegrees;
                y = -Mathf.Abs(t) * arcHeight;
            }

            rt.anchoredPosition = new Vector2(x, y);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            rt.localScale = Vector3.one;
        }

        // CardView 인스턴스의 부모/transform/CanvasGroup을 손패 컨테이너 기준으로 정상화.
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

        // 해당 CardView의 CardInteractable에 interactable 정책을 강제 적용.
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
