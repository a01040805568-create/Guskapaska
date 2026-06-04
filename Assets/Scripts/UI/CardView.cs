using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Core;

namespace Guskapaska.UI
{
    /// <summary>
    /// Visual representation of a single Card. Attached to the CardView prefab.
    /// Stage 6 adds an optional sprite art layer (<c>artImage</c>) that overlays the
    /// placeholder colored background. When a shape/back sprite is missing the view
    /// silently falls back to the UIColors placeholder, so the game stays fully
    /// playable even before card art is imported.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        [Header("Visual Refs")]
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI shapeText;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private GameObject backFace;   // 뒷면 GameObject (face-down 일 때 활성)
        [SerializeField] private GameObject frontFace;  // 앞면 컨테이너 (face-up 일 때 활성)

        [Header("Card Art (Stage 6)")]
        [Tooltip("카드 모양별 일러스트. null이면 UIColors의 색상으로 폴백된다.")]
        [SerializeField] private Sprite scissorsArt;
        [SerializeField] private Sprite rockArt;
        [SerializeField] private Sprite paperArt;
        [SerializeField] private Sprite cardBackArt;

        [Tooltip("아트가 표시될 Image 컴포넌트. 기존 배경 Image 위에 덮어쓰는 방식.")]
        [SerializeField] private Image artImage;

        /// <summary>The Card this view currently displays. Null until Bind is called.</summary>
        public Card BoundCard { get; private set; }

        // 현재 앞면(true)/뒷면(false) 상태.
        // SetFaceUp 과 Bind 가 호출되는 순서에 상관없이 아트 레이어를 올바른 면으로
        // 갱신하기 위해 마지막 face 상태를 기억한다.
        private bool _faceUp = true;

        /// <summary>
        /// Bind this view to a Card instance and refresh its visuals.
        /// Background color and shape/coin texts are derived from the card data,
        /// and the art layer is updated to match the current face state.
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

            // 모양에 따라 배경색과 한글 라벨 설정 (아트 누락 시의 폴백 표현)
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

            // 현재 face 상태에 맞춰 아트 레이어 갱신.
            // (대개 face-up 이지만, SetFaceUp(false) 가 먼저 호출된 AI 카드라면 뒷면 아트가 적용됨)
            ApplyFaceArt(_faceUp);
        }

        /// <summary>
        /// Toggle the visible side of the card. true → front, false → back.
        /// Also swaps the art layer between shape art and card-back art.
        /// </summary>
        public void SetFaceUp(bool faceUp)
        {
            // 마지막 face 상태 기억 (이후 Bind 가 올바른 면 아트를 적용하도록)
            _faceUp = faceUp;

            // 앞/뒤 GameObject 활성 상태 전환
            if (frontFace != null) frontFace.SetActive(faceUp);
            if (backFace != null) backFace.SetActive(!faceUp);

            // 아트 레이어를 새 면에 맞춰 갱신
            ApplyFaceArt(faceUp);
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

            // 기본은 뒷면 표시 — SetFaceUp 내부에서 ApplyFaceArt(false) 가 호출되어
            // 카드 뒷면 아트(또는 폴백)로 정리된다.
            SetFaceUp(false);
        }

        // ─────────────────────────────────────────────────────────────
        // 아트 레이어 처리 (Stage 6)
        // ─────────────────────────────────────────────────────────────

        // 현재 face 상태와 BoundCard 에 맞춰 artImage 의 sprite/활성 여부를 갱신한다.
        // sprite 가 없으면 artImage 를 비활성화하여 배경 색상 폴백이 그대로 보이게 한다.
        private void ApplyFaceArt(bool faceUp)
        {
            // 아트 Image 슬롯이 비어 있으면 아무것도 하지 않음 — 색상 폴백만으로 동작.
            if (artImage == null) return;

            if (!faceUp)
            {
                // 뒷면: 카드 뒷면 아트가 있으면 표시, 없으면 비활성(= 배경 색 폴백).
                if (cardBackArt != null)
                {
                    artImage.sprite = cardBackArt;
                    artImage.enabled = true;
                }
                else
                {
                    artImage.enabled = false;
                }
                return;
            }

            // 앞면: 바인딩된 카드의 모양 아트를 표시.
            Sprite shapeArt = (BoundCard != null) ? ResolveShapeArt(BoundCard.Shape) : null;
            if (shapeArt != null)
            {
                artImage.sprite = shapeArt;
                artImage.enabled = true;
            }
            else
            {
                // 해당 모양 아트가 없으면 비활성 → 배경 색상 + 라벨 텍스트 폴백.
                artImage.enabled = false;
            }
        }

        // 카드 모양에 해당하는 sprite 를 반환. 슬롯이 비어 있으면 null.
        private Sprite ResolveShapeArt(CardShape shape)
        {
            switch (shape)
            {
                case CardShape.Scissors: return scissorsArt;
                case CardShape.Rock:     return rockArt;
                case CardShape.Paper:    return paperArt;
                default:                 return null;
            }
        }
    }
}
