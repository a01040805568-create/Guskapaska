using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Core;

namespace Guskapaska.UI
{
    /// <summary>
    /// Visual representation of a single Card. Attached to the CardView prefab.
    /// Stage 3 uses placeholder colored rectangles and TMP text; final art lands in Stage 6.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        [Header("Visual Refs")]
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI shapeText;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private GameObject backFace;   // 뒷면 GameObject (face-down 일 때 활성)
        [SerializeField] private GameObject frontFace;  // 앞면 컨테이너 (face-up 일 때 활성)

        /// <summary>The Card this view currently displays. Null until Bind is called.</summary>
        public Card BoundCard { get; private set; }

        /// <summary>
        /// Bind this view to a Card instance and refresh its visuals.
        /// Background color and shape/coin texts are derived from the card data.
        /// </summary>
        public void Bind(Card card)
        {
            // 바인딩되는 카드 저장
            BoundCard = card;

            if (card == null)
            {
                // 카드가 없으면 초기 상태로 되돌리고 종료
                Clear();
                return;
            }

            // 모양에 따라 배경색과 한글 라벨 설정
            switch (card.Shape)
            {
                case CardShape.Scissors:
                    background.color = UIColors.Scissors;
                    shapeText.text = "가위";
                    break;
                case CardShape.Rock:
                    background.color = UIColors.Rock;
                    shapeText.text = "바위";
                    break;
                case CardShape.Paper:
                    background.color = UIColors.Paper;
                    shapeText.text = "보자기";
                    break;
            }

            // 코인 숫자는 단순 정수로 표기
            coinText.text = card.CoinValue.ToString();
        }

        /// <summary>
        /// Toggle the visible side of the card. true → front, false → back.
        /// </summary>
        public void SetFaceUp(bool faceUp)
        {
            // 앞/뒤 GameObject 활성 상태 전환
            if (frontFace != null) frontFace.SetActive(faceUp);
            if (backFace != null) backFace.SetActive(!faceUp);
        }

        /// <summary>
        /// Reset this view to an empty state, hiding the front and showing the back.
        /// </summary>
        public void Clear()
        {
            // 바인딩 해제 및 텍스트/색상 초기화
            BoundCard = null;

            if (shapeText != null) shapeText.text = string.Empty;
            if (coinText != null) coinText.text = string.Empty;
            if (background != null) background.color = UIColors.CardBack;

            // 기본은 뒷면 표시
            SetFaceUp(false);
        }
    }
}