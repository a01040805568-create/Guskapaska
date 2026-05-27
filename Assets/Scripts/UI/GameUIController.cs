using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Core;
using Guskapaska.Game;

namespace Guskapaska.UI
{
    /// <summary>
    /// The single subscriber to <see cref="GameEvents"/> in the UI layer.
    /// Bridges runtime game events to the view components and forwards player
    /// input (drag-and-drop submissions) back to <see cref="GameManager"/>.
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

        [Header("Top Bar")]
        [Tooltip("상단의 라운드 표시 TMP 텍스트. OnRoundStarted/OnMatchStarted에서 갱신된다.")]
        [SerializeField] private TextMeshProUGUI roundLabel;

        [Header("Drag")]
        [SerializeField] private DragController dragController;

        private bool _subscribed;

        private void Start()
        {
            if (!ValidateRefs())
            {
                return;
            }

            // 코인 그리드는 게임 규칙상 13칸으로 초기화 (00_GameDesign.md §5).
            coinGridView.Initialize();

            SubscribeEvents();

            // §18: 구독 시점에 이미 발화된 MatchStarted 이벤트를 놓쳤을 수 있으므로
            // 현재 GameState를 직접 읽어 UI를 강제 동기화.
            if (gameManager != null && gameManager.State != null)
            {
                OnMatchStarted(gameManager.State);
            }
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        // ─────────────────────────────────────────────────────────────
        // 유효성 검사
        // ─────────────────────────────────────────────────────────────

        private bool ValidateRefs()
        {
            bool ok = true;

            if (gameManager == null)         { Debug.LogError("[GameUIController] gameManager 가 연결되지 않았습니다."); ok = false; }
            if (playerHandView == null)      { Debug.LogError("[GameUIController] playerHandView 가 연결되지 않았습니다."); ok = false; }
            if (aiHandView == null)          { Debug.LogError("[GameUIController] aiHandView 가 연결되지 않았습니다."); ok = false; }
            if (coinGridView == null)        { Debug.LogError("[GameUIController] coinGridView 가 연결되지 않았습니다."); ok = false; }
            if (playerGemPile == null)       { Debug.LogError("[GameUIController] playerGemPile 이 연결되지 않았습니다."); ok = false; }
            if (aiGemPile == null)           { Debug.LogError("[GameUIController] aiGemPile 이 연결되지 않았습니다."); ok = false; }
            if (submissionZone == null)      { Debug.LogError("[GameUIController] submissionZone 이 연결되지 않았습니다."); ok = false; }
            if (timerView == null)           { Debug.LogError("[GameUIController] timerView 가 연결되지 않았습니다."); ok = false; }
            if (drawAccumulator == null)     { Debug.LogError("[GameUIController] drawAccumulator 가 연결되지 않았습니다."); ok = false; }
            if (resultPanel == null)         { Debug.LogError("[GameUIController] resultPanel 이 연결되지 않았습니다."); ok = false; }
            if (dragController == null)      { Debug.LogError("[GameUIController] dragController 가 연결되지 않았습니다."); ok = false; }
            // roundLabel은 선택 사항(Optional). 연결되어 있지 않으면 라운드 표시만 비활성화되고 게임은 진행된다.

            return ok;
        }

        // ─────────────────────────────────────────────────────────────
        // 이벤트 구독 / 해제
        // ─────────────────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (_subscribed || gameManager == null || gameManager.Events == null)
            {
                return;
            }

            GameEvents events = gameManager.Events;
            events.OnMatchStarted += OnMatchStarted;
            events.OnRoundStarted += OnRoundStarted;
            events.OnTimerTick += OnTimerTick;
            events.OnCountdownTriggered += OnCountdownTriggered;
            events.OnPlayerCardSubmitted += OnPlayerCardSubmitted;
            events.OnAiCardSubmitted += OnAiCardSubmitted;
            events.OnRoundResolved += OnRoundResolved;
            events.OnDrawAccumulatorChanged += OnDrawAccumulatorChanged;
            events.OnGemsChanged += OnGemsChanged;
            events.OnMatchEnded += OnMatchEnded;

            if (dragController != null)
            {
                dragController.OnPlayerCardSubmitted += OnPlayerCardDropped;
            }

            _subscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_subscribed || gameManager == null || gameManager.Events == null)
            {
                return;
            }

            GameEvents events = gameManager.Events;
            events.OnMatchStarted -= OnMatchStarted;
            events.OnRoundStarted -= OnRoundStarted;
            events.OnTimerTick -= OnTimerTick;
            events.OnCountdownTriggered -= OnCountdownTriggered;
            events.OnPlayerCardSubmitted -= OnPlayerCardSubmitted;
            events.OnAiCardSubmitted -= OnAiCardSubmitted;
            events.OnRoundResolved -= OnRoundResolved;
            events.OnDrawAccumulatorChanged -= OnDrawAccumulatorChanged;
            events.OnGemsChanged -= OnGemsChanged;
            events.OnMatchEnded -= OnMatchEnded;

            if (dragController != null)
            {
                dragController.OnPlayerCardSubmitted -= OnPlayerCardDropped;
            }

            _subscribed = false;
        }

        // ─────────────────────────────────────────────────────────────
        // 게임 이벤트 핸들러
        // ─────────────────────────────────────────────────────────────

        private void OnMatchStarted(GameState state)
        {
            // 양쪽 손패 렌더링 (HandView가 자체 faceUp 필드로 앞/뒷면 결정).
            playerHandView.Render(state.PlayerHand.Cards);
            aiHandView.Render(state.AiHand.Cards);

            // 보석/코인 상태 초기화.
            playerGemPile.SetCount(0);
            aiGemPile.SetCount(0);
            coinGridView.SetRemaining(state.CenterGems);

            // 라운드 종속 뷰들 초기화.
            submissionZone.Clear();
            drawAccumulator.SetCoins(0);
            resultPanel.Hide();

            // 매치 시작 시점에 라운드 라벨을 1로 강제 표시.
            // OnRoundStarted가 곧이어 호출되며 정식 값으로 덮어쓰지만,
            // 강제 동기화 시점(Start 끝)에 한 프레임이라도 빈 상태로 두지 않기 위함.
            UpdateRoundLabel(1);

            // 손패가 새로 렌더링됐으므로 드래그 등록.
            if (dragController != null)
            {
                dragController.RegisterPlayerCards();
            }
        }

        private void OnRoundStarted(int roundNumber)
        {
            submissionZone.Clear();
            Debug.Log($"[UI] Round {roundNumber} started");

            // 상단의 "라운드 N" 표시를 갱신.
            UpdateRoundLabel(roundNumber);

            // 새 라운드 시작 → 카드 다시 드래그 가능.
            if (dragController != null)
            {
                dragController.SetAllInteractable(true);
            }
        }

        private void OnTimerTick(float secondsRemaining)
        {
            timerView.SetTime(secondsRemaining);
            timerView.SetUrgent(secondsRemaining <= 3f);
        }

        private void OnCountdownTriggered()
        {
            // Stage 5에서 3-2-1 오버레이로 대체될 예정.
            Debug.Log("[UI] Countdown triggered");
        }

        private void OnPlayerCardSubmitted(Card card)
        {
            submissionZone.ShowPlayerCard(card);

            // 카드가 손에서 제거됐으므로 손패를 다시 그린다.
            playerHandView.Render(gameManager.State.PlayerHand.Cards);

            // 새로 그린 카드 인스턴스에 드래그 이벤트 재등록.
            if (dragController != null)
            {
                dragController.RegisterPlayerCards();
                // 제출 직후에는 라운드가 끝날 때까지 드래그 불가.
                dragController.SetAllInteractable(false);
            }
        }

        private void OnAiCardSubmitted(Card card)
        {
            submissionZone.ShowAiCard(card);
            aiHandView.Render(gameManager.State.AiHand.Cards);
        }

        private void OnRoundResolved(RoundOutcome outcome)
        {
            // 라운드 결과로 패자에게 카드가 넘어갔을 수 있으므로 양쪽 손패 모두 재렌더링.
            playerHandView.Render(gameManager.State.PlayerHand.Cards);
            aiHandView.Render(gameManager.State.AiHand.Cards);

            // 새 손패의 카드들도 드래그 가능하도록 등록.
            // (실제 활성화/비활성화는 OnRoundStarted / OnPlayerCardSubmitted에서 토글)
            if (dragController != null)
            {
                dragController.RegisterPlayerCards();
            }
        }

        private void OnDrawAccumulatorChanged(int coins)
        {
            drawAccumulator.SetCoins(coins);
        }

        private void OnGemsChanged(int player, int ai, int center)
        {
            playerGemPile.SetCount(player);
            aiGemPile.SetCount(ai);
            coinGridView.SetRemaining(center);
        }

        private void OnMatchEnded(MatchResult result)
        {
            // 매치 종료 → 모든 드래그 비활성화.
            if (dragController != null)
            {
                dragController.SetAllInteractable(false);
            }

            resultPanel.Show(result);
        }

        // ─────────────────────────────────────────────────────────────
        // 드래그 → 제출 흐름
        // ─────────────────────────────────────────────────────────────

        private void OnPlayerCardDropped(CardInteractable card)
        {
            // 드롭된 카드의 데이터를 GameManager로 전달.
            if (card == null || card.CardView == null || card.CardView.BoundCard == null)
            {
                return;
            }

            Card boundCard = card.CardView.BoundCard;
            gameManager.OnPlayerSubmit(boundCard);

            // 이후 흐름은 게임 이벤트로 이어진다:
            // GameManager → RoundController.SubmitPlayerCard → OnPlayerCardSubmitted 이벤트
            // → OnPlayerCardSubmitted 핸들러에서 손패 재렌더링 + SetAllInteractable(false)
        }

        // ─────────────────────────────────────────────────────────────
        // 내부 유틸
        // ─────────────────────────────────────────────────────────────

        // 라운드 라벨 텍스트를 갱신. 라벨이 Inspector에 연결되어 있지 않으면 무시.
        private void UpdateRoundLabel(int roundNumber)
        {
            if (roundLabel == null) return;
            roundLabel.text = $"라운드 {roundNumber}";
        }
    }
}