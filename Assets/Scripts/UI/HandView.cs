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
        /// Also reclaims any pooled view whose transform was reparented outside the
        /// container (e.g. mid-drag cards that ended up under the Canvas root), to
        /// guarantee that no orphaned card visuals are left behind after a submission.
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
            // 또한 드래그 도중 부모가 Canvas 루트로 옮겨진 채 풀에 남은 뷰가 있을 수 있으므로
            // 부모를 cardContainer로 강제 복구해 화면에 떠 있는 잔상을 방지한다.
            for (int i = cards.Count; i < _activeViews.Count; i++)
            {
                CardView leftoverView = _activeViews[i];
                if (leftoverView == null)
                {
                    continue;
                }

                // 부모가 컨테이너 밖이라면 다시 컨테이너 아래로 회수.
                // worldPositionStays=false: 어차피 곧 비활성화되므로 좌표 보존이 불필요.
                if (leftoverView.transform.parent != cardContainer)
                {
                    leftoverView.transform.SetParent(cardContainer, worldPositionStays: false);
                }

                leftoverView.gameObject.SetActive(false);
            }

            // 각 카드 바인딩 및 위치 계산
            int total = cards.Count;
            // 가로 중앙 정렬을 위한 시작 오프셋 계산
            float startX = -((total - 1) * cardSpacing) * 0.5f;

            for (int i = 0; i < total; i++)
            {
                CardView view = _activeViews[i];
                view.gameObject.SetActive(true);

                // ★ 드래그 등으로 부모가 Canvas 루트로 이동된 뷰가 있을 수 있다.
                // 이 경우 anchoredPosition만 갱신해도 카드는 손패 위치로 돌아오지 않는다.
                // 따라서 부모를 cardContainer로 강제 복구한 뒤 위치/회전/스케일을 잡는다.
                if (view.transform.parent != cardContainer)
                {
                    view.transform.SetParent(cardContainer, worldPositionStays: false);
                }

                // 손패는 평면 배치이므로 sibling 순서도 재정렬해 시각적 z-order를 일관되게 유지.
                view.transform.SetSiblingIndex(i);

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
        /// Any view that was reparented away from the container (e.g. mid-drag) is
        /// returned to the container so the pool remains in a clean state.
        /// </summary>
        public void Clear()
        {
            // 모든 뷰 비활성화 및 바인딩 해제.
            // 드래그 도중 Canvas 루트로 옮겨진 뷰가 있다면 부모도 함께 복구한다.
            foreach (CardView view in _activeViews)
            {
                if (view == null)
                {
                    continue;
                }

                if (view.transform.parent != cardContainer)
                {
                    view.transform.SetParent(cardContainer, worldPositionStays: false);
                }

                view.Clear();
                view.gameObject.SetActive(false);
            }
        }
    }
}
