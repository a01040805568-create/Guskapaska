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
        [Tooltip("상단의 라운드 표시 TMP 텍스트.")]
        [SerializeField] private TextMeshProUGUI roundLabel;

        [Header("Drag")]
        [SerializeField] private DragController dragController;

        private bool _subscribed;

        // 슬라이드 애니메이션이 진행 중인 카드. OnPlayerCardSubmitted가 발화되면 그 카드의 데이터는
        // 슬라이드 코루틴이 처리할 것이므로 즉시 ShowPlayerCard를 호출하면 안 된다 (이중 표시 방지).
        private CardInteractable _animatingSubmission;

        private void Start()
        {
            if (!ValidateRefs())
            {
                return;
            }

            coinGridView.Initialize();

            SubscribeEvents();

            // §18: 구독 시점에 이미 발화된 MatchStarted를 놓쳤을 수 있으므로 강제 동기화.
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
            playerHandView.Render(state.PlayerHand.Cards);
            aiHandView.Render(state.AiHand.Cards);

            playerGemPile.SetCount(0);
            aiGemPile.SetCount(0);
            coinGridView.SetRemaining(state.CenterGems);

            submissionZone.Clear();
            drawAccumulator.SetCoins(0);
            resultPanel.Hide();

            UpdateRoundLabel(1);

            if (dragController != null)
            {
                dragController.RegisterPlayerCards();
            }
        }

        private void OnRoundStarted(int roundNumber)
        {
            submissionZone.Clear();
            Debug.Log($"[UI] Round {roundNumber} started");

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
            Debug.Log("[UI] Countdown triggered");
        }

        private void OnPlayerCardSubmitted(Card card)
        {
            // 슬라이드 애니메이션 중이라면 코루틴이 ShowPlayerCard를 호출할 책임을 진다.
            if (_animatingSubmission == null)
            {
                submissionZone.ShowPlayerCard(card);
            }

            // 손패 재렌더링.
            playerHandView.Render(gameManager.State.PlayerHand.Cards);

            if (dragController != null)
            {
                dragController.RegisterPlayerCards();
                // 안전망 — 이미 OnPlayerCardDropped에서 SetAllInteractable(false)했지만,
                // 새로 그려진 카드 인스턴스에 대해서도 명시적으로 차단 상태 유지.
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
            // 양쪽 손패 재렌더링 (패자 카드 이동 등 반영).
            playerHandView.Render(gameManager.State.PlayerHand.Cards);
            aiHandView.Render(gameManager.State.AiHand.Cards);

            if (dragController != null)
            {
                dragController.RegisterPlayerCards();
            }

            // 슬라이드 추적 상태 초기화.
            _animatingSubmission = null;
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
            if (card == null || card.CardView == null || card.CardView.BoundCard == null)
            {
                return;
            }

            Card boundCard = card.CardView.BoundCard;

            // 핵심 변경 (버그 2 해결):
            // gameManager.OnPlayerSubmit 호출은 그 안에서 AI 카드 제출까지 동기적으로 진행되며,
            // 그 과정에서 AI HandView가 잠시 재렌더링되어 AI 카드가 활성 상태가 될 수 있다.
            // 따라서 게임 로직 호출 BEFORE에 모든 카드를 즉시 입력 차단해야 한다.
            if (dragController != null)
            {
                dragController.SetAllInteractable(false);
            }

            // 슬라이드 추적 마커.
            _animatingSubmission = card;

            // 시각 슬라이드 시작 (게임 로직과 별개로 진행).
            StartCoroutine(submissionZone.AnimatePlayerCardSubmission(boundCard, card.transform));

            // 게임 로직 진행.
            gameManager.OnPlayerSubmit(boundCard);
        }

        // ─────────────────────────────────────────────────────────────
        // 내부 유틸
        // ─────────────────────────────────────────────────────────────

        private void UpdateRoundLabel(int roundNumber)
        {
            if (roundLabel == null) return;
            roundLabel.text = $"라운드 {roundNumber}";
        }
    }
}