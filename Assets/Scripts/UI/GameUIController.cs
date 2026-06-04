using System;
using System.Collections;
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

        [Header("Animators")]
        [Tooltip("AI 카드 포물선 비행 애니메이션을 담당하는 컴포넌트.")]
        [SerializeField] private AiSubmitAnimator aiSubmitAnimator;

        [Tooltip("보석 비행 + 마리오 손 애니메이션을 담당하는 컴포넌트.")]
        [SerializeField] private GemFlightAnimator gemFlightAnimator;

        [Header("Match Start Animation")]
        [SerializeField] private bool useDealAnimationAtMatchStart = true;

        private bool _subscribed;
        private CardInteractable _animatingSubmission;

        // 보석 비행 애니메이션 동기화를 위해 마지막으로 화면에 반영된 카운트를 추적.
        // OnGemsChanged 시 이 값과 새 값의 차이로 누가 얼마를 가져갔는지 계산.
        private int _lastDisplayedPlayerGems;
        private int _lastDisplayedAiGems;
        private int _lastDisplayedCenterGems;

        private void Start()
        {
            if (!ValidateRefs())
            {
                return;
            }

            coinGridView.Initialize();

            SubscribeEvents();

            // §18: 강제 동기화.
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

            if (gameManager == null) { Debug.LogError("[GameUIController] gameManager 가 연결되지 않았습니다."); ok = false; }
            if (playerHandView == null) { Debug.LogError("[GameUIController] playerHandView 가 연결되지 않았습니다."); ok = false; }
            if (aiHandView == null) { Debug.LogError("[GameUIController] aiHandView 가 연결되지 않았습니다."); ok = false; }
            if (coinGridView == null) { Debug.LogError("[GameUIController] coinGridView 가 연결되지 않았습니다."); ok = false; }
            if (playerGemPile == null) { Debug.LogError("[GameUIController] playerGemPile 이 연결되지 않았습니다."); ok = false; }
            if (aiGemPile == null) { Debug.LogError("[GameUIController] aiGemPile 이 연결되지 않았습니다."); ok = false; }
            if (submissionZone == null) { Debug.LogError("[GameUIController] submissionZone 이 연결되지 않았습니다."); ok = false; }
            if (timerView == null) { Debug.LogError("[GameUIController] timerView 가 연결되지 않았습니다."); ok = false; }
            if (drawAccumulator == null) { Debug.LogError("[GameUIController] drawAccumulator 가 연결되지 않았습니다."); ok = false; }
            if (resultPanel == null) { Debug.LogError("[GameUIController] resultPanel 이 연결되지 않았습니다."); ok = false; }
            if (dragController == null) { Debug.LogError("[GameUIController] dragController 가 연결되지 않았습니다."); ok = false; }
            // aiSubmitAnimator, gemFlightAnimator는 선택사항 — 없으면 즉시 표시 폴백.

            return ok;
        }

        // ─────────────────────────────────────────────────────────────
        // 이벤트 구독 / 해제
        // ─────────────────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (_subscribed || gameManager == null || gameManager.Events == null) return;

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
            if (!_subscribed || gameManager == null || gameManager.Events == null) return;

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
            // 보석/코인 상태 초기화.
            playerGemPile.SetCount(0);
            aiGemPile.SetCount(0);
            coinGridView.SetRemaining(state.CenterGems);

            // 비행 동기화용 상태 캐시.
            _lastDisplayedPlayerGems = 0;
            _lastDisplayedAiGems = 0;
            _lastDisplayedCenterGems = state.CenterGems;

            submissionZone.Clear();
            drawAccumulator.SetCoins(0);
            resultPanel.Hide();

            UpdateRoundLabel(1);

            if (useDealAnimationAtMatchStart)
            {
                if (dragController != null)
                {
                    dragController.SetAllInteractable(false);
                }

                StartCoroutine(DealMatchStartHands(state));
            }
            else
            {
                playerHandView.Render(state.PlayerHand.Cards);
                aiHandView.Render(state.AiHand.Cards);

                if (dragController != null)
                {
                    dragController.RegisterPlayerCards();
                }
            }
        }

        private IEnumerator DealMatchStartHands(GameState state)
        {
            StartCoroutine(aiHandView.RenderWithDealAnimation(state.AiHand.Cards));
            yield return playerHandView.RenderWithDealAnimation(state.PlayerHand.Cards);

            if (dragController != null)
            {
                dragController.RegisterPlayerCards();
                dragController.SetAllInteractable(true);
            }
        }

        private void OnRoundStarted(int roundNumber)
        {
            submissionZone.Clear();
            Debug.Log($"[UI] Round {roundNumber} started");

            UpdateRoundLabel(roundNumber);

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
            if (_animatingSubmission == null)
            {
                submissionZone.ShowPlayerCard(card);
            }

            playerHandView.Render(gameManager.State.PlayerHand.Cards);

            if (dragController != null)
            {
                dragController.RegisterPlayerCards();
                dragController.SetAllInteractable(false);
            }
        }

        private void OnAiCardSubmitted(Card card)
        {
            aiHandView.Render(gameManager.State.AiHand.Cards);

            if (aiSubmitAnimator != null)
            {
                StartCoroutine(aiSubmitAnimator.AnimateAiSubmit(card,
                    onArrived: () => submissionZone.ShowAiCard(card)));
            }
            else
            {
                submissionZone.ShowAiCard(card);
            }
        }

        private void OnRoundResolved(RoundOutcome outcome)
        {
            playerHandView.Render(gameManager.State.PlayerHand.Cards);
            aiHandView.Render(gameManager.State.AiHand.Cards);

            if (dragController != null)
            {
                dragController.RegisterPlayerCards();
            }

            _animatingSubmission = null;
        }

        private void OnDrawAccumulatorChanged(int coins)
        {
            drawAccumulator.SetCoins(coins);
        }

        private void OnGemsChanged(int player, int ai, int center)
        {
            // 변화량 계산.
            int playerDelta = player - _lastDisplayedPlayerGems;
            int aiDelta = ai - _lastDisplayedAiGems;

            // 누가 가져갔는지 판단. 한 라운드에 한쪽만 가져가므로 두 변화량이 동시에 양수가 될 수 없음.
            // (무승부 → 누구도 가져가지 않음, 변화량 모두 0)
            bool winnerIsPlayer = playerDelta > 0;
            bool winnerIsAi = aiDelta > 0;
            int gemsTaken = Mathf.Max(playerDelta, aiDelta);

            // 비행 애니메이션이 가능한 경우에만 시도.
            if (gemFlightAnimator != null && (winnerIsPlayer || winnerIsAi) && gemsTaken > 0)
            {
                // 새 카운트값을 클로저로 캡처해서 도착 시 적용.
                int newPlayer = player;
                int newAi = ai;
                int newCenter = center;

                gemFlightAnimator.StartGemAcquisition(gemsTaken, winnerIsPlayer, onArrived: () =>
                {
                    playerGemPile.SetCount(newPlayer);
                    aiGemPile.SetCount(newAi);
                    coinGridView.SetRemaining(newCenter);

                    // 도착 시점에 표시된 값을 캐시 갱신.
                    _lastDisplayedPlayerGems = newPlayer;
                    _lastDisplayedAiGems = newAi;
                    _lastDisplayedCenterGems = newCenter;
                });
            }
            else
            {
                // 폴백: 즉시 갱신.
                playerGemPile.SetCount(player);
                aiGemPile.SetCount(ai);
                coinGridView.SetRemaining(center);

                _lastDisplayedPlayerGems = player;
                _lastDisplayedAiGems = ai;
                _lastDisplayedCenterGems = center;
            }
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

            if (dragController != null)
            {
                dragController.SetAllInteractable(false);
            }

            _animatingSubmission = card;

            StartCoroutine(submissionZone.AnimatePlayerCardSubmission(boundCard, card.transform));

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
