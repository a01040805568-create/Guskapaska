using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Core;

namespace Guskapaska.UI
{
    /// <summary>
    /// 카드 한 장의 시각 표현. CardView 프리팹에 부착된다.
    /// Stage 6에서 placeholder 배경색 위에 덮이는 선택적 아트 레이어(artImage)를 추가했다.
    /// 모양/뒷면 스프라이트가 비어 있으면 조용히 UIColors 색상으로 폴백하므로,
    /// 카드 아트를 임포트하기 전에도 게임은 정상 동작한다.
    /// 비워진(Clear) 뷰는 CanvasGroup으로 완전히 투명해져, 중앙 제출 슬롯 같은 빈 슬롯이
    /// 아무것도 보이지 않으면서도 드롭은 계속 받을 수 있다.
    /// 코인 값은 좌측 상단 coinGroup 안에 coinPrefab 인스턴스를 값만큼 생성해 표시한다.
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

        [Header("Coin Display")]
        [Tooltip("코인 아이콘이 생성될 카드 좌측 상단 컨테이너(빈 RectTransform). " +
                 "아트 위에 보이도록 CardView 안에서 artImage보다 뒤(아래) 형제 = 최상위 레이어에 둘 것.")]
        [SerializeField] private RectTransform coinGroup;

        [Tooltip("coinGroup에 코인 값만큼 생성할 기존 코인 프리팹 (예: CoinCell). 새로 만들 필요 없이 기존 것 재사용.")]
        [SerializeField] private GameObject coinPrefab;

        /// <summary>현재 이 뷰가 표시 중인 카드. Bind 호출 전에는 null.</summary>
        public Card BoundCard { get; private set; }

        // 현재 앞면(true)/뒷면(false) 상태.
        private bool _faceUp = true;

        // 현재 바인딩된 카드의 코인 값 (표시할 코인 개수). Clear 시 0.
        private int _coinValue;

        // coinGroup에 생성해둔 코인 인스턴스 풀. 표시 개수에 맞춰 활성/비활성으로 재사용.
        private readonly List<GameObject> _coinInstances = new List<GameObject>();

        private void Awake()
        {
            // Inspector에서 지정하지 않았으면 같은 GameObject의 CanvasGroup을 사용.
            // (CardInteractable이 RequireComponent로 CanvasGroup을 보장하므로 보통 존재함)
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// 이 뷰를 카드 인스턴스에 바인딩하고 시각 요소를 갱신한다.
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

            // 코인 값 저장 후 좌측 상단 코인 그룹 갱신 (현재 face 상태 반영)
            _coinValue = card.CoinValue;
            RefreshCoins();

            // 카드가 바인딩됐으니 보이게 한다 (빈 슬롯에서 alpha 0이었을 수 있으므로 복원).
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            // 현재 face 상태에 맞춰 아트 레이어 갱신.
            ApplyFaceArt(_faceUp);
        }

        /// <summary>
        /// 카드의 보이는 면을 전환한다. true → 앞면, false → 뒷면.
        /// </summary>
        public void SetFaceUp(bool faceUp)
        {
            _faceUp = faceUp;

            if (frontFace != null) frontFace.SetActive(faceUp);
            if (backFace != null) backFace.SetActive(!faceUp);

            ApplyFaceArt(faceUp);

            // 코인 그룹은 FrontFace 밖(최상위 레이어)에 있으므로 face 전환 시 직접 갱신.
            // 뒷면이면 코인을 숨긴다.
            RefreshCoins();
        }

        /// <summary>
        /// 이 뷰를 빈 상태로 되돌린다. 완전히 투명하게 만들어 빈 슬롯이 아무것도 보이지 않게 하되,
        /// 활성 상태는 유지해 드롭 감지는 계속 동작하도록 한다.
        /// </summary>
        public void Clear()
        {
            // 바인딩 해제 및 텍스트/색상 초기화
            BoundCard = null;

            if (shapeText != null) shapeText.text = string.Empty;
            if (coinText != null) coinText.text = string.Empty;
            if (background != null) background.color = UIColors.CardBack;

            // 코인 값 0 → 아래 SetFaceUp 안의 RefreshCoins가 전부 숨긴다.
            _coinValue = 0;

            // 구조상 뒷면 상태로 둔다 (face-down). 내부에서 RefreshCoins도 호출됨.
            SetFaceUp(false);

            // 빈 슬롯은 완전히 투명하게 — 카드 뒷면("뒤")이나 배경색이 보이지 않도록.
            // alpha만 0으로 하므로 raycast(드롭 감지)는 유지되어 SetActive(false) 함정을 피한다.
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        // ─────────────────────────────────────────────────────────────
        // 코인 표시 처리
        // ─────────────────────────────────────────────────────────────

        // 앞면이면 coinGroup 안에 코인 인스턴스를 _coinValue 개 표시하고, 뒷면이면 전부 숨긴다.
        // 기존 인스턴스를 재사용(활성/비활성 토글)하고, 부족할 때만 coinPrefab을 추가 생성한다.
        // 코인 그룹이 FrontFace 자식이 아니라 최상위 레이어(artImage 위)라서,
        // 뒷면 가시성은 frontFace.SetActive에 의존하지 않고 여기서 직접 통제한다.
        private void RefreshCoins()
        {
            if (coinGroup == null || coinPrefab == null) return;

            // 뒷면(AI 카드 등)이면 코인 0개 표시.
            int shown = _faceUp ? _coinValue : 0;

            // 필요한 개수만큼 인스턴스를 확보 (부족하면 coinGroup 자식으로 생성).
            while (_coinInstances.Count < shown)
            {
                GameObject coin = Instantiate(coinPrefab, coinGroup);
                _coinInstances.Add(coin);
            }

            // 앞에서부터 shown개만 활성, 나머지는 비활성.
            for (int i = 0; i < _coinInstances.Count; i++)
            {
                if (_coinInstances[i] != null)
                {
                    _coinInstances[i].SetActive(i < shown);
                }
            }
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
