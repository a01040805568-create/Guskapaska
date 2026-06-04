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

        [Tooltip("타이머가 3초 이하일 때 화면 중앙에 표시되는 카운트다운 오버레이.")]
        [SerializeField] private CountdownOverlay countdownOverlay;

        [Header("Match Start Animation")]
        [SerializeField] private bool useDealAnimationAtMatchStart = true;

        [Header("Countdown Settings")]
        [Tooltip("이 초 이하로 떨어지면 카운트다운 오버레이가 활성화된다.")]
        [SerializeField] private int countdownThreshold = 3;

        private bool _subscribed;
        private CardInteractable _animatingSubmission;

        private int _lastDisplayedPlayerGems;
        private int _lastDisplayedAiGems;
        private int _lastDisplayedCenterGems;

        // 카운트다운 오버레이 중복 호출 방지용 — 마지막으로 표시한 정수 초.
        // OnTimerTick은 매 프레임 호출되므로 정수 초가 바뀌는 순간에만 트리거해야 한다.
        private int _lastCountdownNumber = int.MaxValue;

        private void Start()
        {
            if (!ValidateRefs())
            {
                return;
            }

            coinGridView.Initialize();

            SubscribeEvents();

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
            // aiSubmitAnimator, gemFlightAnimator, countdownOverlay는 선택사항.

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
            playerGemPile.SetCount(0);
            aiGemPile.SetCount(0);
            coinGridView.SetRemaining(state.CenterGems);

            _lastDisplayedPlayerGems = 0;
            _lastDisplayedAiGems = 0;
            _lastDisplayedCenterGems = state.CenterGems;

            submissionZone.Clear();
            drawAccumulator.SetCoins(0);
            resultPanel.Hide();

            UpdateRoundLabel(1);

            // 카운트다운 추적 상태 초기화.
            _lastCountdownNumber = int.MaxValue;
            if (countdownOverlay != null)
            {
                countdownOverlay.HideInstant();
            }

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

            // 새 라운드 시작 → 카운트다운 추적 상태도 초기화 (이전 라운드의 1초가 다음 라운드 14초로
            // 점프하면서 카운트다운이 다시 트리거되도록).
            _lastCountdownNumber = int.MaxValue;
            if (countdownOverlay != null)
            {
                countdownOverlay.HideInstant();
            }

            if (dragController != null)
            {
                dragController.SetAllInteractable(true);
            }
        }

        private void OnTimerTick(float secondsRemaining)
        {
            timerView.SetTime(secondsRemaining);
            timerView.SetUrgent(secondsRemaining <= 3f);

            // 카운트다운 오버레이 트리거.
            // TimerView와 동일한 올림 처리로 정수 초를 계산.
            int displaySeconds = secondsRemaining < 0f ? 0 : Mathf.CeilToInt(secondsRemaining);

            // 임계값(기본 3) 이하이고, 이전 프레임의 표시 초와 다를 때만 호출.
            // 0초는 표시하지 않음 (시간 만료 시점에 큰 0이 뜨는 건 어색함).
            if (displaySeconds <= countdownThreshold
                && displaySeconds > 0
                && displaySeconds != _lastCountdownNumber)
            {
                _lastCountdownNumber = displaySeconds;
                if (countdownOverlay != null)
                {
                    countdownOverlay.ShowNumber(displaySeconds);
                }
            }
        }

        private void OnCountdownTriggered()
        {
            // GameEvents의 OnCountdownTriggered는 별도의 트리거 이벤트.
            // 현재 구현은 OnTimerTick에서 직접 카운트다운을 띄우므로 여기서는 로그만.
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
            int playerDelta = player - _lastDisplayedPlayerGems;
            int aiDelta = ai - _lastDisplayedAiGems;

            bool winnerIsPlayer = playerDelta > 0;
            bool winnerIsAi = aiDelta > 0;
            int gemsTaken = Mathf.Max(playerDelta, aiDelta);

            if (gemFlightAnimator != null && (winnerIsPlayer || winnerIsAi) && gemsTaken > 0)
            {
                int newPlayer = player;
                int newAi = ai;
                int newCenter = center;

                gemFlightAnimator.StartGemAcquisition(gemsTaken, winnerIsPlayer, onArrived: () =>
                {
                    playerGemPile.SetCount(newPlayer);
                    aiGemPile.SetCount(newAi);
                    coinGridView.SetRemaining(newCenter);

                    _lastDisplayedPlayerGems = newPlayer;
                    _lastDisplayedAiGems = newAi;
                    _lastDisplayedCenterGems = newCenter;
                });
            }
            else
            {
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

            // 매치 종료 시 카운트다운 오버레이는 즉시 숨김.
            if (countdownOverlay != null)
            {
                countdownOverlay.HideInstant();
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

            // 드래그 입력 즉시 차단 (AI 카드 빼앗기 방지).
            if (dragController != null)
            {
                dragController.SetAllInteractable(false);
            }

            _animatingSubmission = card;

            // 드래그된 카드의 현재 월드 위치와 스케일을 슬라이드 시작점으로 사용.
            Vector3 startWorldPos = card.transform.position;
            Vector3 startScale = card.transform.localScale;

            // 시각 슬라이드 시작 (임시 카드 인스턴스가 슬라이드, 원본 카드는 건드리지 않음).
            StartCoroutine(submissionZone.AnimatePlayerCardSubmission(boundCard, startWorldPos, startScale));

            // 게임 로직 진행 — 손패에서 카드 제거 + AI 자동 제출.
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
