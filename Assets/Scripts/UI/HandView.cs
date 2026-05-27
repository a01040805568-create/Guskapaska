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
        [SerializeField] private float cardSpacing = 80f;     // 카드 간 가로 간격 (겹침 포함)
        [SerializeField] private float arcAngleDegrees = 0f;  // Stage 3에서는 0 (직선 배치). Stage 5에서 부채꼴로 확장
        [SerializeField] private float arcHeight = 0f;        // 동일 사유로 0

        // 현재 화면에 표시 중인 CardView 인스턴스 풀
        private readonly List<CardView> _activeViews = new List<CardView>();

        /// <summary>현재 화면에 표시 중인 CardView들의 읽기 전용 목록.</summary>
        public IReadOnlyList<CardView> ActiveViews => _activeViews;

        /// <summary>이 손패가 드래그/호버 입력을 받는지 여부. AI 손패 식별 등에 활용.</summary>
        public bool Interactable => interactable;

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

                // 새로 생성된 카드에 대해 입력 가능 여부를 즉시 설정.
                // 같은 CardView 프리팹이 플레이어/AI 양쪽 손패에서 재사용되므로
                // 각 HandView가 자기 카드의 입력 정책을 강제해야 한다.
                ApplyInteractableToView(view);
            }

            // 남는 뷰는 비활성화 (Destroy 대신 SetActive로 재사용 가능하게 유지)
            for (int i = cards.Count; i < _activeViews.Count; i++)
            {
                // 비활성화하기 전에 부모/transform을 정상 상태로 복귀시킨다.
                // 드래그 도중 부모가 Canvas root로 옮겨졌거나 transform이 흐트러진 경우에도
                // 다음에 재사용될 때 깨끗한 상태에서 시작하도록 보장.
                ReclaimToContainer(_activeViews[i]);
                _activeViews[i].gameObject.SetActive(false);
            }

            // 각 카드 바인딩 및 위치 계산
            int total = cards.Count;
            // 가로 중앙 정렬을 위한 시작 오프셋 계산
            float startX = -((total - 1) * cardSpacing) * 0.5f;

            for (int i = 0; i < total; i++)
            {
                CardView view = _activeViews[i];

                // 핵심: 재사용 중인 카드의 부모가 cardContainer가 아닐 수 있다.
                // - 드래그 중 CardInteractable.OnBeginDrag에서 부모를 Canvas root로 옮긴 카드
                // - 외부 코드가 transform을 건드린 경우
                // Render는 손패의 시각적 권위자이므로, 누가 어디로 옮겼든 다시 컨테이너로 가져와
                // anchoredPosition/회전/스케일/CanvasGroup 상태까지 모두 정상 상태로 복구한다.
                ReclaimToContainer(view);

                view.gameObject.SetActive(true);
                view.Bind(cards[i]);
                view.SetFaceUp(faceUp);

                // 재사용되는 뷰에도 다시 한 번 입력 정책을 적용.
                ApplyInteractableToView(view);

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
                // Clear 시점에도 부모를 컨테이너로 되돌려놓아야 다음 재사용이 안전하다.
                ReclaimToContainer(view);
                view.Clear();
                view.gameObject.SetActive(false);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 내부 유틸
        // ─────────────────────────────────────────────────────────────

        // CardView 인스턴스의 부모/transform/CanvasGroup을 손패 컨테이너 기준으로 정상화한다.
        // 드래그 도중 부모가 Canvas root로 옮겨져 있거나 raycast 차단이 풀리지 않은 상태여도
        // 이 호출 후엔 깨끗한 상태로 복귀한다.
        private void ReclaimToContainer(CardView view)
        {
            if (view == null || cardContainer == null) return;

            // 부모가 다르면 다시 가져온다. worldPositionStays=false로 즉시 컨테이너 기준 좌표계로 진입.
            // 위치/회전/스케일은 호출자(Render)가 곧바로 덮어쓸 것이므로 여기서는 부모만 정리한다.
            if (view.transform.parent != cardContainer)
            {
                view.transform.SetParent(cardContainer, worldPositionStays: false);
            }

            // 드래그 중간에 OnEndDrag가 호출되지 않은 채로 강제 재렌더링되는 경우를 대비해
            // CanvasGroup의 raycast 차단을 풀어둔다. (이미 풀려 있다면 무해)
            CanvasGroup cg = view.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = true;
                cg.alpha = 1f;
            }
        }

        // 해당 CardView의 CardInteractable에 interactable 정책을 강제 적용한다.
        // AI 손패는 interactable=false로 설정되어 호버/드래그 입력이 모두 차단된다.
        private void ApplyInteractableToView(CardView view)
        {
            if (view == null) return;

            CardInteractable ci = view.GetComponent<CardInteractable>();
            if (ci == null)
            {
                // CardView 프리팹에 CardInteractable이 없는 경우 정책 적용 불가 — 무시.
                return;
            }

            // Interactable 플래그로 호버/드래그 가드 (CardInteractable 내부 가드 처리).
            ci.Interactable = interactable;

            // 컴포넌트 자체를 비활성화하면 OnPointerEnter 등 이벤트 메서드 자체가 호출되지 않아
            // 더 견고하다.
            ci.enabled = interactable;
        }
    }
}