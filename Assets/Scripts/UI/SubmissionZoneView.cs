using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Core;
using Guskapaska.Util;

namespace Guskapaska.UI
{
    /// <summary>
    /// Visualizes the area where both submitted cards are revealed each round.
    /// The player slot stays active throughout the match because it doubles as
    /// the drop target for player card submissions (see <see cref="DropZone"/>).
    /// Only its visual content is cleared between rounds.
    /// Stage 5 adds a smooth slide animation when the player submits a card.
    /// </summary>
    public class SubmissionZoneView : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField] private CardView playerSlot;
        [SerializeField] private CardView aiSlot;

        [Header("Player Slot Visual")]
        [Tooltip("PlayerSlot의 background Image 참조. 빈 상태일 때 알파를 0으로 만들어 CardBack 색이 보이지 않게 한다.")]
        [SerializeField] private Image playerSlotBackground;

        [Tooltip("PlayerSlot 자식의 FrontFace GameObject. 빈 상태일 때 함께 비활성화.")]
        [SerializeField] private GameObject playerSlotFrontFace;

        [Tooltip("PlayerSlot 자식의 BackFace GameObject. 빈 상태일 때 함께 비활성화하여 보라색이 보이지 않게 한다.")]
        [SerializeField] private GameObject playerSlotBackFace;

        [Header("Submission Animation")]
        [Tooltip("플레이어 카드가 드롭 위치에서 PlayerSlot으로 슬라이드되는 시간(초).")]
        [SerializeField] private float submissionSlideDuration = 0.3f;

        [Tooltip("슬라이드와 함께 카드 스케일을 변화시킬 때 사용. 드래그 중인 스케일(예: 1.2)에서 1.0으로 줄어든다.")]
        [SerializeField] private float submissionEndScale = 1f;

        private void Awake()
        {
            // 시작 시 두 슬롯 모두 빈 상태로 초기화.
            Clear();
        }

        private void OnDisable()
        {
            // 진행 중인 슬라이드 트윈 정리.
            TweenRunner.CancelAll(this);
        }

        /// <summary>
        /// Display the player's submitted card face-up in the player slot.
        /// Used when no animation is needed (e.g. fallback path, initial sync).
        /// </summary>
        public void ShowPlayerCard(Card card)
        {
            if (playerSlot == null) return;

            // 배경 알파 복원. Clear()에서 0으로 설정되어 있을 수 있다.
            SetPlayerSlotBackgroundAlpha(1f);

            // FrontFace는 다시 켜고, BackFace는 CardView.SetFaceUp(true)가 알아서 처리.
            if (playerSlotFrontFace != null) playerSlotFrontFace.SetActive(true);

            playerSlot.Bind(card);
            playerSlot.SetFaceUp(true);
        }

        /// <summary>
        /// Animate the source card (the one the player just dropped) sliding into the
        /// player slot, then commit the slot's visual content to the bound card.
        /// </summary>
        /// <param name="card">The card data to display at the end of the animation.</param>
        /// <param name="sourceTransform">The transform of the GameObject currently representing
        /// the dragged card (likely under Canvas root after OnBeginDrag re-parented it).
        /// On completion, this GameObject is hidden because <paramref name="playerSlot"/>
        /// takes over the visual representation.</param>
        public IEnumerator AnimatePlayerCardSubmission(Card card, Transform sourceTransform)
        {
            if (playerSlot == null || sourceTransform == null)
            {
                // 어느 한쪽이라도 누락되면 안전한 폴백: 즉시 표시 후 종료.
                ShowPlayerCard(card);
                yield break;
            }

            // 1) 슬라이드 시작 전 PlayerSlot 자체는 시각적으로 비어있어야 한다.
            //    (드래그한 카드가 도착하기 전이라 보라색이나 빈 카드가 잠깐 보이면 어색함)
            //    Clear() 호출이 처리하는 알파 0 + FrontFace/BackFace 비활성을 그대로 유지.

            // 2) 슬라이드 대상 transform의 부모를 PlayerSlot의 부모와 같은 캔버스로 옮긴다.
            //    이미 Canvas root에 있을 가능성이 높으므로 그대로 두고, 월드 좌표로 트윈한다.
            //    (Canvas root와 PlayerSlot이 다른 부모를 가질 수 있어 localPosition 트윈은 위험)

            // 슬라이드 목적지: PlayerSlot의 월드 위치.
            Vector3 endWorldPos = playerSlot.transform.position;
            Vector3 startWorldPos = sourceTransform.position;

            // RectTransform 기반 UI라 position(world) 트윈도 안전하게 동작한다.
            // 별도 헬퍼가 없으므로 인라인 코루틴으로 구현 (월드 좌표용).
            // TweenRunner.MoveLocal은 localPosition만 다루므로 여기서는 직접 보간한다.

            float elapsed = 0f;
            float duration = submissionSlideDuration;
            AnimationCurve curve = EasingCurves.EaseOutQuad;

            // 스케일도 함께 줄어들도록 시작/종료 값 기록.
            Vector3 startScale = sourceTransform.localScale;
            Vector3 endScale = Vector3.one * submissionEndScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float k = curve.Evaluate(u);

                // 월드 좌표 보간.
                sourceTransform.position = Vector3.LerpUnclamped(startWorldPos, endWorldPos, k);
                sourceTransform.localScale = Vector3.LerpUnclamped(startScale, endScale, k);

                // 도중에 트랜스폼이 파괴되면 안전하게 종료.
                if (sourceTransform == null)
                {
                    yield break;
                }

                yield return null;
            }

            // 3) PlayerSlot에 실제 카드 데이터를 바인딩하여 표시.
            //    슬라이드 GameObject는 이후 즉시 비활성화되어 PlayerSlot이 시각 권한을 인수.
            ShowPlayerCard(card);

            // 4) 슬라이드용 GameObject 비활성화. 손패 풀에서 재사용되는 인스턴스이므로 파괴 금지.
            //    HandView의 ReclaimToContainer가 다음 Render 시점에 부모/스케일/알파를 정리할 것.
            if (sourceTransform != null && sourceTransform.gameObject != null)
            {
                sourceTransform.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Display the AI's submitted card face-up in the AI slot.
        /// </summary>
        public void ShowAiCard(Card card)
        {
            if (aiSlot == null) return;

            // 슬롯 오브젝트 활성화 후 카드 바인딩.
            aiSlot.gameObject.SetActive(true);
            aiSlot.Bind(card);
            aiSlot.SetFaceUp(true);
        }

        /// <summary>
        /// Reset both slots. Called at the start of each round and after the result delay.
        /// </summary>
        public void Clear()
        {
            // PlayerSlot은 DropZone이 부착되어 있어 드롭 입력을 받아야 하므로
            // GameObject 자체는 끄지 않고 시각적 내용만 비운다.
            if (playerSlot != null)
            {
                playerSlot.Clear();
            }

            // CardView.Clear()는 내부적으로 SetFaceUp(false)를 호출하여 BackFace를 켠다.
            // 그러나 빈 슬롯에서 BackFace의 보라색이 보이면 부자연스러우므로,
            // FrontFace와 BackFace를 모두 비활성화하여 완전히 투명한 슬롯으로 만든다.
            if (playerSlotFrontFace != null) playerSlotFrontFace.SetActive(false);
            if (playerSlotBackFace != null) playerSlotBackFace.SetActive(false);

            SetPlayerSlotBackgroundAlpha(0f);

            // AiSlot은 드롭 대상이 아니므로 기존처럼 비활성화하여 완전히 숨긴다.
            if (aiSlot != null)
            {
                aiSlot.Clear();
                aiSlot.gameObject.SetActive(false);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 내부 유틸
        // ─────────────────────────────────────────────────────────────

        // PlayerSlot 배경 Image의 알파만 변경. raycastTarget은 그대로 유지된다.
        private void SetPlayerSlotBackgroundAlpha(float alpha)
        {
            if (playerSlotBackground == null) return;

            Color c = playerSlotBackground.color;
            c.a = alpha;
            playerSlotBackground.color = c;
        }
    }
}