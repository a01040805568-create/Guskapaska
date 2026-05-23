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

        [Header("Fan Layout")]
        [SerializeField] private float cardSpacing = 80f;     // 카드 간 가로 간격 (겹침 포함)
        [SerializeField] private float arcAngleDegrees = 0f;  // Stage 3에서는 0 (직선 배치). Stage 5에서 부채꼴로 확장
        [SerializeField] private float arcHeight = 0f;        // 동일 사유로 0

        // 현재 화면에 표시 중인 CardView 인스턴스 풀
        private readonly List<CardView> _activeViews = new List<CardView>();

        /// <summary>현재 화면에 표시 중인 CardView들의 읽기 전용 목록.</summary>
        public IReadOnlyList<CardView> ActiveViews => _activeViews;

        /// <summary>
        /// Render the given cards. Reuses existing CardView instances to minimize churn.
        /// </summary>
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
            }

            // 남는 뷰는 비활성화 (Destroy 대신 SetActive로 재사용 가능하게 유지)
            for (int i = cards.Count; i < _activeViews.Count; i++)
            {
                _activeViews[i].gameObject.SetActive(false);
            }

            // 각 카드 바인딩 및 위치 계산
            int total = cards.Count;
            // 가로 중앙 정렬을 위한 시작 오프셋 계산
            float startX = -((total - 1) * cardSpacing) * 0.5f;

            for (int i = 0; i < total; i++)
            {
                CardView view = _activeViews[i];
                view.gameObject.SetActive(true);
                view.Bind(cards[i]);
                view.SetFaceUp(faceUp);

                // Stage 3에서는 직선 배치만 사용 (arcAngleDegrees/arcHeight는 모두 0)
                RectTransform rt = view.GetComponent<RectTransform>();
                float x = startX + i * cardSpacing;
                float y = 0f;
                float angle = 0f;

                // arcAngleDegrees가 0이 아니어도 Stage 3에서는 결과적으로 0 적용. Stage 5 확장 지점.
                if (Mathf.Abs(arcAngleDegrees) > 0.0001f && total > 1)
                {
                    float t = (i / (float)(total - 1)) - 0.5f; // -0.5 ~ 0.5
                    angle = -t * arcAngleDegrees;
                    y = -Mathf.Abs(t) * arcHeight;
                }

                rt.anchoredPosition = new Vector2(x, y);
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
                rt.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Hide all active CardViews without destroying them.
        /// </summary>
        public void Clear()
        {
            // 모든 뷰 비활성화 및 바인딩 해제
            foreach (CardView view in _activeViews)
            {
                view.Clear();
                view.gameObject.SetActive(false);
            }
        }
    }
}