using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Guskapaska.UI
{
    /// <summary>
    /// Visualizes the accumulated draw coins (e.g. "× 4").
    /// The container is hidden when the accumulator is 0.
    /// </summary>
    public class DrawAccumulatorView : MonoBehaviour
    {
        [SerializeField] private GameObject container;          // 0이면 비활성화
        [SerializeField] private TextMeshProUGUI multiplierText; // "× 4"
        [SerializeField] private Image coinIcon;

        /// <summary>
        /// Update the accumulator display. Hides the container when coins is 0.
        /// </summary>
        public void SetCoins(int coins)
        {
            // 0 이하일 경우 컨테이너 비활성화 (시각적으로 숨김)
            if (coins <= 0)
            {
                if (container != null) container.SetActive(false);
                return;
            }

            // 값이 있을 때만 컨테이너 활성화 및 텍스트 갱신
            if (container != null) container.SetActive(true);
            if (multiplierText != null)
            {
                multiplierText.text = "× " + coins.ToString();
            }
        }
    }
}