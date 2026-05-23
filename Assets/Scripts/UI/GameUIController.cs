using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Core;
using Guskapaska.Game;

namespace Guskapaska.UI
{
    /// <summary>
    /// Single subscriber to GameEvents in Stage 3.
    /// Translates game events into view updates across all UI components in the Game scene.
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;

        [Header("Views")]
        [SerializeField] private HandView playerHandView;
        [SerializeField] private HandView aiHandView;
        [SerializeField] private CoinGridView coinGridView;
        [SerializeField] private GemPileView playerGemPile;
        [SerializeField] private GemPileView aiGemPile;
        [SerializeField] private SubmissionZoneView submissionZone;
        [SerializeField] private TimerView timerView;
        [SerializeField] private DrawAccumulatorView drawAccumulator;
        [SerializeField] private ResultPanelController resultPanel;

        [Header("Debug (removed in Stage 4)")]
        [SerializeField] private Button debugSubmitButton;   // 첫 카드 자동 제출

        // 구독 중복 방지를 위한 플래그
        private bool _subscribed;

        private void Start()
{
    // 직렬화된 참조 유효성 검사
    if (!ValidateRefs()) return;

    // 코인 그리드 초기화 (13셀 생성)
    coinGridView.Initialize();

    // GameEvents 이벤트 구독
    SubscribeEvents();

    // 디버그 제출 버튼 이벤트 연결
    if (debugSubmitButton != null)
    {
        debugSubmitButton.onClick.AddListener(OnDebugSubmitClicked);
    }

    // ⭐ 추가: 구독 시점에 이미 발화된 이벤트를 놓쳤을 수 있으므로,
    // 현재 GameState를 직접 읽어 UI를 강제 동기화한다.
    if (gameManager != null && gameManager.State != null)
    {
        OnMatchStarted(gameManager.State);
    }
}

        private void OnDisable()
        {
            // 이벤트 누수 방지를 위해 항상 구독 해제
            UnsubscribeEvents();

            if (debugSubmitButton != null)
            {
                debugSubmitButton.onClick.RemoveListener(OnDebugSubmitClicked);
            }
        }

        // 필수 참조가 누락되지 않았는지 확인
        private bool ValidateRefs()
{
    bool ok = true;

    if (gameManager == null) { Debug.LogError("[GameUIController] gameManager 참조 누락"); ok = false; }
    if (playerHandView == null) { Debug.LogError("[GameUIController] playerHandView 참조 누락"); ok = false; }
    if (aiHandView == null) { Debug.LogError("[GameUIController] aiHandView 참조 누락"); ok = false; }
    if (coinGridView == null) { Debug.LogError("[GameUIController] coinGridView 참조 누락"); ok = false; }
    if (playerGemPile == null) { Debug.LogError("[GameUIController] playerGemPile 참조 누락"); ok = false; }
    if (aiGemPile == null) { Debug.LogError("[GameUIController] aiGemPile 참조 누락"); ok = false; }
    if (submissionZone == null) { Debug.LogError("[GameUIController] submissionZone 참조 누락"); ok = false; }
    if (timerView == null) { Debug.LogError("[GameUIController] timerView 참조 누락"); ok = false; }
    if (drawAccumulator == null) { Debug.LogError("[GameUIController] drawAccumulator 참조 누락"); ok = false; }
    
    // resultPanel은 Branch 4에서 추가되므로 경고만, 진행은 허용
    if (resultPanel == null) { Debug.LogWarning("[GameUIController] resultPanel 참조 누락 (Branch 4에서 추가 예정)"); }

    return ok;
}

        // 모든 GameEvents 구독
        private void SubscribeEvents()
        {
            if (_subscribed || gameManager == null || gameManager.Events == null) return;

            GameEvents e = gameManager.Events;
            e.OnMatchStarted          += OnMatchStarted;
            e.OnRoundStarted          += OnRoundStarted;
            e.OnTimerTick             += OnTimerTick;
            e.OnCountdownTriggered    += OnCountdownTriggered;
            e.OnPlayerCardSubmitted   += OnPlayerCardSubmitted;
            e.OnAiCardSubmitted       += OnAiCardSubmitted;
            e.OnRoundResolved         += OnRoundResolved;
            e.OnDrawAccumulatorChanged += OnDrawAccumulatorChanged;
            e.OnGemsChanged           += OnGemsChanged;
            e.OnMatchEnded            += OnMatchEnded;

            _subscribed = true;
        }

        // 모든 GameEvents 해제
        private void UnsubscribeEvents()
        {
            if (!_subscribed || gameManager == null || gameManager.Events == null)
            {
                _subscribed = false;
                return;
            }

            GameEvents e = gameManager.Events;
            e.OnMatchStarted          -= OnMatchStarted;
            e.OnRoundStarted          -= OnRoundStarted;
            e.OnTimerTick             -= OnTimerTick;
            e.OnCountdownTriggered    -= OnCountdownTriggered;
            e.OnPlayerCardSubmitted   -= OnPlayerCardSubmitted;
            e.OnAiCardSubmitted       -= OnAiCardSubmitted;
            e.OnRoundResolved         -= OnRoundResolved;
            e.OnDrawAccumulatorChanged -= OnDrawAccumulatorChanged;
            e.OnGemsChanged           -= OnGemsChanged;
            e.OnMatchEnded            -= OnMatchEnded;

            _subscribed = false;
        }

        // ---------- Event handlers ----------

        private void OnMatchStarted(GameState state)
{
    playerHandView.Render(state.PlayerHand.Cards);
    aiHandView.Render(state.AiHand.Cards);
    playerGemPile.SetCount(state.PlayerGems);
    aiGemPile.SetCount(state.AiGems);
    coinGridView.SetRemaining(state.CenterGems);
    submissionZone.Clear();
    drawAccumulator.SetCoins(0);
    
    // resultPanel null 체크
    if (resultPanel != null)
    {
        resultPanel.Hide();
    }
}

        private void OnRoundStarted(int roundNumber)
        {
            // 새 라운드 시작 시 제출 영역 비우기
            submissionZone.Clear();

            // Stage 3에서는 라운드 번호를 로그로만 확인
            Debug.Log("[UI] 라운드 " + roundNumber + " 시작");
        }

        private void OnTimerTick(float secondsRemaining)
        {
            // 매 프레임 시간 갱신 및 3초 이하 시 긴급 색상 전환
            timerView.SetTime(secondsRemaining);
            timerView.SetUrgent(secondsRemaining <= 3f);
        }

        private void OnCountdownTriggered()
        {
            // Stage 3에서는 단순 로그만, 3-2-1 오버레이는 Stage 5
            Debug.Log("[UI] 카운트다운 발동");
        }

        private void OnPlayerCardSubmitted(Card card)
        {
            // 제출 영역에 플레이어 카드 표시
            submissionZone.ShowPlayerCard(card);

            // 손패에서 제거된 상태를 반영 (state는 이미 갱신됨)
            if (gameManager != null && gameManager.State != null)
            {
                playerHandView.Render(gameManager.State.PlayerHand.Cards);
            }
        }

        private void OnAiCardSubmitted(Card card)
        {
            // 제출 영역에 AI 카드 표시
            submissionZone.ShowAiCard(card);

            // AI 손패도 동일하게 갱신
            if (gameManager != null && gameManager.State != null)
            {
                aiHandView.Render(gameManager.State.AiHand.Cards);
            }
        }

        private void OnRoundResolved(RoundOutcome outcome)
        {
            // Stage 3에서는 손패 변동을 반영 (패배 측에 카드가 추가됨)
            if (gameManager != null && gameManager.State != null)
            {
                playerHandView.Render(gameManager.State.PlayerHand.Cards);
                aiHandView.Render(gameManager.State.AiHand.Cards);
            }

            // Stage 5에서 이곳에 리빌 애니메이션이 추가될 예정
        }

        private void OnDrawAccumulatorChanged(int coins)
        {
            // 누적 코인 표시 갱신 (0이면 자동으로 숨김)
            drawAccumulator.SetCoins(coins);
        }

        private void OnGemsChanged(int player, int ai, int center)
        {
            // 양쪽 보석 더미와 중앙 코인 그리드 갱신
            playerGemPile.SetCount(player);
            aiGemPile.SetCount(ai);
            coinGridView.SetRemaining(center);
        }

        private void OnMatchEnded(MatchResult result)
{
    // resultPanel이 없을 수 있으므로 null 체크
    if (resultPanel != null)
    {
        resultPanel.Show(result);
    }
}

        // 디버그 제출: 플레이어 손패의 첫 번째 카드를 제출 (Stage 4에서 드래그앤드롭으로 대체)
        private void OnDebugSubmitClicked()
{
    if (gameManager == null || gameManager.State == null) return;
    if (gameManager.State.PlayerHand.IsEmpty) return;

    Card first = gameManager.State.PlayerHand.GetAt(0);
    gameManager.OnPlayerSubmit(first);
}
    }
}