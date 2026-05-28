using System.Collections.Generic;
using UnityEngine;
using Guskapaska.Core;

namespace Guskapaska.UI
{
    /// <summary>
    /// Renders a row of cards for either the player or the AI hand.
    /// Stage 3 uses a straight-line layout; the fan layout is added in Stage 5.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private GameObject cardViewPrefab;
        [SerializeField] private bool faceUp = true;          // 플레이어 핸드는 true, AI 핸드는 false

        [Header("Interaction")]
        [Tooltip("이 손패의 카드들이 호버/드래그 입력을 받는지 여부. 플레이어 핸드는 true, AI 핸드는 false.")]
        [SerializeField] private bool interactable = true;

        [Header("Fan Layout")]
        [SerializeField] private float cardSpacing = 80f;
        [SerializeField] private float arcAngleDegrees = 0f;
        [SerializeField] private float arcHeight = 0f;

        private readonly List<CardView> _activeViews = new List<CardView>();

        public IReadOnlyList<CardView> ActiveViews => _activeViews;
        public bool Interactable => interactable;

        public void Render(IReadOnlyList<Card> cards)
        {
            if (cards == null)
            {
                Clear();
                return;
            }

            // 부족한 만큼 새로 생성
            while (_activeViews.Count < cards.Count)
            {
                GameObject go = Instantiate(cardViewPrefab, cardContainer);
                CardView view = go.GetComponent<CardView>();
                _activeViews.Add(view);
                ApplyInteractableToView(view);
            }

            // 남는 뷰는 비활성화
            for (int i = cards.Count; i < _activeViews.Count; i++)
            {
                ReclaimToContainer(_activeViews[i]);
                _activeViews[i].gameObject.SetActive(false);
            }

            int total = cards.Count;
            float startX = -((total - 1) * cardSpacing) * 0.5f;

            for (int i = 0; i < total; i++)
            {
                CardView view = _activeViews[i];

                // 부모를 컨테이너로 강제 복귀 (드래그 도중 떨어진 카드 회수).
                ReclaimToContainer(view);

                view.gameObject.SetActive(true);
                view.Bind(cards[i]);
                view.SetFaceUp(faceUp);

                // 입력 정책 재적용.
                ApplyInteractableToView(view);

                // 위치 / 회전 / 스케일 설정
                RectTransform rt = view.GetComponent<RectTransform>();
                float x = startX + i * cardSpacing;
                float y = 0f;
                float angle = 0f;

                if (Mathf.Abs(arcAngleDegrees) > 0.0001f && total > 1)
                {
                    float t = (i / (float)(total - 1)) - 0.5f;
                    angle = -t * arcAngleDegrees;
                    y = -Mathf.Abs(t) * arcHeight;
                }

                rt.anchoredPosition = new Vector2(x, y);
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
                rt.localScale = Vector3.one;

                // 핵심: 카드의 transform이 최종 위치에 안착했으므로, 이 시점에 rest 상태로 캡처.
                // 호버 / 드래그 / 복귀가 모두 이 위치로 돌아온다.
                // CardInteractable이 있는 경우에만 호출 (AI 손패도 OK — Awake에서 입력 정책으로 차단됨).
                CardInteractable ci = view.GetComponent<CardInteractable>();
                if (ci != null)
                {
                    ci.CaptureRestState();
                }
            }
        }

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