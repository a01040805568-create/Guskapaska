using TMPro;
using UnityEngine;

namespace Guskapaska.UI
{
    /// <summary>
    /// Visualizes the round countdown. The 3-2-1 center overlay belongs to Stage 5.
    /// </summary>
    public class TimerView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timeText;       // 큰 숫자
        [SerializeField] private TextMeshProUGUI labelText;      // "남은 시간"

        private void Awake()
        {
            // 라벨은 정적이라 한 번만 설정
            if (labelText != null)
            {
                labelText.text = "남은 시간";
            }
        }

        /// <summary>
        /// Display the remaining seconds, rounded up to a whole number.
        /// </summary>
        public void SetTime(float secondsRemaining)
        {
            if (timeText == null) return;

            // 음수는 0으로 보정 후 올림 처리
            if (secondsRemaining < 0f) secondsRemaining = 0f;
            int display = Mathf.CeilToInt(secondsRemaining);
            timeText.text = display.ToString();
        }

        /// <summary>
        /// Toggle the urgent color (red) for the last 3 seconds.
        /// </summary>
        public void SetUrgent(bool urgent)
        {
            if (timeText == null) return;

            // 긴급 상태일 때 빨강, 그 외는 흰색
            timeText.color = urgent ? UIColors.TimerUrgent : UIColors.TimerNormal;
        }
    }
}