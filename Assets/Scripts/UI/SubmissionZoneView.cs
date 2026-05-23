using UnityEngine;
using Guskapaska.Core;

namespace Guskapaska.UI
{
    /// <summary>
    /// Visualizes the area where both submitted cards are revealed each round.
    /// </summary>
    public class SubmissionZoneView : MonoBehaviour
    {
        [SerializeField] private CardView playerSlot;
        [SerializeField] private CardView aiSlot;

        private void Awake()
        {
            // 시작 시 두 슬롯 모두 빈 상태로 초기화
            Clear();
        }

        /// <summary>
        /// Display the player's submitted card face-up in the player slot.
        /// </summary>
        public void ShowPlayerCard(Card card)
        {
            if (playerSlot == null) return;

            // 슬롯 오브젝트 활성화 후 카드 바인딩
            playerSlot.gameObject.SetActive(true);
            playerSlot.Bind(card);
            playerSlot.SetFaceUp(true);
        }

        /// <summary>
        /// Display the AI's submitted card face-up in the AI slot.
        /// </summary>
        public void ShowAiCard(Card card)
        {
            if (aiSlot == null) return;

            // 슬롯 오브젝트 활성화 후 카드 바인딩
            aiSlot.gameObject.SetActive(true);
            aiSlot.Bind(card);
            aiSlot.SetFaceUp(true);
        }

        /// <summary>
        /// Reset both slots. Called at the start of each round and after the result delay.
        /// </summary>
        public void Clear()
        {
            // 두 슬롯 모두 빈 상태로 되돌리고 시각적으로 숨김
            if (playerSlot != null)
            {
                playerSlot.Clear();
                playerSlot.gameObject.SetActive(false);
            }
            if (aiSlot != null)
            {
                aiSlot.Clear();
                aiSlot.gameObject.SetActive(false);
            }
        }
    }
}