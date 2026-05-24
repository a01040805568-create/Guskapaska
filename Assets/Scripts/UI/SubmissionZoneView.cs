using UnityEngine;
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
        [SerializeField] private CardView playerSlot;
        [SerializeField] private CardView aiSlot;

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
            // CardView.Clear()가 텍스트를 비우고 배경을 CardBack 색으로 만들어 빈 슬롯처럼 보이게 한다.
            if (playerSlot != null)
            {
                playerSlot.Clear();
            }

            // AiSlot은 드롭 대상이 아니므로 기존처럼 비활성화하여 완전히 숨긴다.
            if (aiSlot != null)
            {
                aiSlot.Clear();
                aiSlot.gameObject.SetActive(false);
            }
        }
    }
}