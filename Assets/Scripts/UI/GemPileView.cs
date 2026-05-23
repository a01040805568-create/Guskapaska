using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Guskapaska.UI
{
    /// <summary>
    /// Visualizes a single player's collected gem count.
    /// Stage 3 shows a static icon and number. Stage 5 will animate gems flying into here.
    /// </summary>
    public class GemPileView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Image icon;

        /// <summary>
        /// Update the displayed gem count.
        /// </summary>
        public void SetCount(int count)
        {
            // 음수는 0으로 보정
            if (count < 0) count = 0;

            if (countText != null)
            {
                countText.text = count.ToString();
            }
        }
    }
}