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
    /// Stage 5 adds a smooth slide animation using a spawned temporary CardView
    /// instance so the original hand card stays under HandView's control.
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

        [Header("Slide Animation Setup")]
        [Tooltip("슬라이드 중에만 표시되는 임시 카드 프리팹. AiSubmitAnimator의 FlyingCard 프리팹을 그대로 재사용 가능.")]
        [SerializeField] private GameObject flyingCardPrefab;

        [Tooltip("임시 슬라이드 카드가 잠시 살 부모. 보통 메인 Canvas의 RectTransform.")]
        [SerializeField] private RectTransform flyContainer;

        private void Awake()
        {
            // 시작 시 두 슬롯 모두 빈 상태로 초기화.
            Clear();
        }

        private void OnDisable()
        {
            TweenRunner.CancelAll(this);
        }

        /// <summary>
        /// Display the player's submitted card face-up in the player slot.
        /// Used when no animation is needed (e.g. fallback path, initial sync).
        /// </summary>
        public void ShowPlayerCard(Card card)
        {
            if (playerSlot == null) return;

            SetPlayerSlotBackgroundAlpha(1f);

            if (playerSlotFrontFace != null) playerSlotFrontFace.SetActive(true);

            playerSlot.Bind(card);
            playerSlot.SetFaceUp(true);
        }

        /// <summary>
        /// Spawn a temporary CardView and slide it from the drop position to the player slot.
        /// On arrival, the slot itself is committed to the bound card and the temporary
        /// instance is destroyed. The original hand card is never touched by this method
        /// so that HandView retains full control over the hand pool.
        /// </summary>
        /// <param name="card">The card data to display at the end of the animation.</param>
        /// <param name="startWorldPos">World position where the slide should begin
        /// (typically the drop position).</param>
        /// <param name="startScale">The local scale to begin the slide at (typically 1.2x
        /// since the player was dragging).</param>
        public IEnumerator AnimatePlayerCardSubmission(Card card, Vector3 startWorldPos, Vector3 startScale)
        {
            // 필수 참조 누락 시 폴백: 즉시 표시 후 종료.
            if (playerSlot == null || flyingCardPrefab == null || flyContainer == null)
            {
                ShowPlayerCard(card);
                yield break;
            }

            // 1) 임시 슬라이드 카드 인스턴스 생성.
            GameObject flyingGo = Instantiate(flyingCardPrefab, flyContainer);
            RectTransform flyingRt = flyingGo.GetComponent<RectTransform>();
            if (flyingRt == null)
            {
                Destroy(flyingGo);
                ShowPlayerCard(card);
                yield break;
            }

            // 2) 임시 카드는 face-up으로 카드 내용 표시 (플레이어가 어떤 카드를 냈는지 보임).
            CardView flyingView = flyingGo.GetComponent<CardView>();
            if (flyingView != null)
            {
                flyingView.Bind(card);
                flyingView.SetFaceUp(true);
            }

            // 3) 시작 위치/스케일 설정. flyContainer 기준 로컬 좌표로 변환.
            Vector3 startLocal = flyContainer.InverseTransformPoint(startWorldPos);
            flyingRt.localPosition = startLocal;
            flyingRt.localScale = startScale;
            flyingRt.localRotation = Quaternion.identity;

            // 도착 위치 — PlayerSlot의 월드 좌표를 flyContainer 로컬로 변환.
            Vector3 endWorldPos = playerSlot.transform.position;
            Vector3 endLocal = flyContainer.InverseTransformPoint(endWorldPos);
            Vector3 endScale = Vector3.one * submissionEndScale;

            // 4) 슬라이드 트윈.
            float elapsed = 0f;
            AnimationCurve curve = EasingCurves.EaseOutQuad;

            while (elapsed < submissionSlideDuration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / submissionSlideDuration);
                float k = curve.Evaluate(u);

                flyingRt.localPosition = Vector3.LerpUnclamped(startLocal, endLocal, k);
                flyingRt.localScale = Vector3.LerpUnclamped(startScale, endScale, k);

                if (flyingRt == null)
                {
                    yield break;
                }
                yield return null;
            }

            // 5) 도착 — PlayerSlot에 실제 카드 표시.
            ShowPlayerCard(card);

            // 6) 임시 인스턴스 파괴.
            if (flyingGo != null)
            {
                Destroy(flyingGo);
            }
        }

        /// <summary>
        /// Display the AI's submitted card face-up in the AI slot.
        /// </summary>
        public void ShowAiCard(Card card)
        {
            if (aiSlot == null) return;

            aiSlot.gameObject.SetActive(true);
            aiSlot.Bind(card);
            aiSlot.SetFaceUp(true);
        }

        /// <summary>
        /// Reset both slots. Called at the start of each round and after the result delay.
        /// </summary>
        public void Clear()
        {
            if (playerSlot != null)
            {
                playerSlot.Clear();
            }

            if (playerSlotFrontFace != null) playerSlotFrontFace.SetActive(false);
            if (playerSlotBackFace != null) playerSlotBackFace.SetActive(false);

            SetPlayerSlotBackgroundAlpha(0f);

            if (aiSlot != null)
            {
                aiSlot.Clear();
                aiSlot.gameObject.SetActive(false);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 내부 유틸
        // ─────────────────────────────────────────────────────────────

        private void SetPlayerSlotBackgroundAlpha(float alpha)
        {
            if (playerSlotBackground == null) return;

            Color c = playerSlotBackground.color;
            c.a = alpha;
            playerSlotBackground.color = c;
        }
    }
}
