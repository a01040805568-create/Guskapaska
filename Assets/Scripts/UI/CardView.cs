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
    /// An empty (cleared) view is made fully transparent via a CanvasGroup so empty
    /// slots — e.g. the center submission slot — show nothing while still receiving drops.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        [Header("Visual Refs")]
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI shapeText;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private GameObject backFace;   // 뒷면 GameObject (face-down 일 때 활성)
        [SerializeField] private GameObject frontFace;  // 앞면 컨테이너 (face-up 일 때 활성)

        [Tooltip("빈 슬롯을 투명하게 만들기 위한 CanvasGroup. 비워두면 CardView 루트에서 자동으로 찾는다.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Card Art (Stage 6)")]
        [Tooltip("카드 모양별 일러스트. null이면 UIColors의 색상으로 폴백된다.")]
        [SerializeField] private Sprite scissorsArt;
        [SerializeField] private Sprite rockArt;
        [SerializeField] private Sprite paperArt;
        [SerializeField] private Sprite cardBackArt;

        [Tooltip("앞면 아트가 표시될 Image 컴포넌트. 기존 배경 Image 위에 덮어쓰는 방식.")]
        [SerializeField] private Image artImage;

        /// <summary>The Card this view currently displays. Null until Bind is called.</summary>
        public Card BoundCard { get; private set; }

        // 현재 앞면(true)/뒷면(false) 상태.
        private bool _faceUp = true;

        private void Awake()
        {
            // Inspector에서 지정하지 않았으면 같은 GameObject의 CanvasGroup을 사용.
            // (CardInteractable이 RequireComponent로 CanvasGroup을 보장하므로 보통 존재함)
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Bind this view to a Card instance and refresh its visuals.
        /// </summary>
        public void Bind(Card card)
        {
            // 바인딩되는 카드 저장
            BoundCard = card;

            if (card == null)
            {
                // 카드가 없으면 빈 상태로 정리하고 종료
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

            // 코인 숫자 표기
            coinText.text = card.CoinValue.ToString();

            // 카드가 바인딩됐으니 보이게 한다 (빈 슬롯에서 alpha 0이었을 수 있으므로 복원).
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            // 현재 face 상태에 맞춰 아트 레이어 갱신.
            ApplyFaceArt(_faceUp);
        }

        /// <summary>
        /// Toggle the visible side of the card. true → front, false → back.
        /// </summary>
        public void SetFaceUp(bool faceUp)
        {
            _faceUp = faceUp;

            if (frontFace != null) frontFace.SetActive(faceUp);
            if (backFace != null) backFace.SetActive(!faceUp);

            ApplyFaceArt(faceUp);
        }

        /// <summary>
        /// Reset this view to an empty state. The view is made fully transparent so an
        /// empty slot shows nothing, while staying active so drop detection keeps working.
        /// </summary>
        public void Clear()
        {
            // 바인딩 해제 및 텍스트/색상 초기화
            BoundCard = null;

            if (shapeText != null) shapeText.text = string.Empty;
            if (coinText != null) coinText.text = string.Empty;
            if (background != null) background.color = UIColors.CardBack;

            // 구조상 뒷면 상태로 둔다 (face-down).
            SetFaceUp(false);

            // 빈 슬롯은 완전히 투명하게 — 카드 뒷면("뒤")이나 배경색이 보이지 않도록.
            // alpha만 0으로 하므로 raycast(드롭 감지)는 유지되어 SetActive(false) 함정을 피한다.
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        // ─────────────────────────────────────────────────────────────
        // 아트 레이어 처리 (Stage 6)
        // ─────────────────────────────────────────────────────────────

        // 현재 face 상태와 BoundCard 에 맞춰 artImage 의 sprite/활성 여부를 갱신.
        // artImage 하나가 앞면(모양 아트)과 뒷면(cardBackArt)을 모두 담당한다.
        private void ApplyFaceArt(bool faceUp)
        {
            if (artImage == null) return;

            if (!faceUp)
            {
                // 뒷면: 카드 뒷면 아트(cardBackArt)가 있으면 표시, 없으면 비활성(배경색 폴백).
                // 빈 슬롯도 이 경로를 타지만, Clear()에서 alpha를 0으로 만들어 보이지 않게 한다.
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
                case CardShape.Rock: return rockArt;
                case CardShape.Paper: return paperArt;
                default: return null;
            }
        }
    }
}
