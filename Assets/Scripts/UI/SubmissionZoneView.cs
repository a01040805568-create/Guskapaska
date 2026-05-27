using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Core;

namespace Guskapaska.UI
{
    /// <summary>
    /// Visualizes the area where both submitted cards are revealed each round.
    /// The player slot stays active throughout the match because it doubles as
    /// the drop target for player card submissions (see <see cref="DropZone"/>).
    /// Only its visual content is cleared between rounds.
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

        private void Awake()
        {
            // 시작 시 두 슬롯 모두 빈 상태로 초기화.
            Clear();
        }

        /// <summary>
        /// Display the player's submitted card face-up in the player slot.
        /// </summary>
        public void ShowPlayerCard(Card card)
        {
            if (playerSlot == null) return;

            // 배경 알파 복원. Clear()에서 0으로 설정되어 있을 수 있다.
            SetPlayerSlotBackgroundAlpha(1f);

            // FrontFace는 다시 켜고, BackFace는 CardView.SetFaceUp(true)가 알아서 처리.
            // 안전을 위해 FrontFace를 미리 활성화해둔다.
            if (playerSlotFrontFace != null) playerSlotFrontFace.SetActive(true);

            // PlayerSlot은 항상 활성 상태이므로 바인딩만 수행하면 된다.
            playerSlot.Bind(card);
            playerSlot.SetFaceUp(true);
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
            // root Image의 raycast는 그대로 동작하므로 드롭 입력에는 영향 없다.
            if (playerSlotFrontFace != null) playerSlotFrontFace.SetActive(false);
            if (playerSlotBackFace != null) playerSlotBackFace.SetActive(false);

            // root 배경 Image의 알파도 0으로 만들어 CardBack 색이 보이지 않게 한다.
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
