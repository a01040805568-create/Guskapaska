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

        [Header("Match Start Animation")]
        [Tooltip("매치 시작 시 카드 딜 애니메이션을 사용할지 여부. true면 RenderWithDealAnimation 사용.")]
        [SerializeField] private bool useDealAnimationAtMatchStart = true;

        private bool _subscribed;
        private CardInteractable _animatingSubmission;

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
            // aiSubmitAnimator는 선택사항 — 없으면 즉시 표시로 폴백.

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

            // 라운드 종속 뷰들 초기화.
            submissionZone.Clear();
            drawAccumulator.SetCoins(0);
            resultPanel.Hide();

            UpdateRoundLabel(1);

            // 손패 렌더링 — 딜 애니메이션 사용 여부에 따라 분기.
            if (useDealAnimationAtMatchStart)
            {
                // 딜 애니메이션 중에는 드래그 입력 차단.
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

        // 매치 시작 시 양쪽 손패를 동시에 딜 + 종료 후 드래그 등록.
        private IEnumerator DealMatchStartHands(GameState state)
        {
            // 양쪽을 동시에 시작하되 플레이어 손패 종료를 기준으로 대기.
            // (양쪽 카드 수와 stagger 설정이 같다면 동시에 끝남.)
            StartCoroutine(aiHandView.RenderWithDealAnimation(state.AiHand.Cards));
            yield return playerHandView.RenderWithDealAnimation(state.PlayerHand.Cards);

            // 딜 종료 후 드래그 등록.
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
            // 슬라이드 애니메이션 중이라면 코루틴이 ShowPlayerCard를 호출할 책임을 진다.
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
            // AI 손패에서 카드 하나를 즉시 제거. 비행 카드는 별도 인스턴스.
            aiHandView.Render(gameManager.State.AiHand.Cards);

            // AiSubmitAnimator가 있으면 비행 애니메이션 시작.
            // 비행 도착 시 SubmissionZoneView.ShowAiCard를 콜백으로 호출.
            if (aiSubmitAnimator != null)
            {
                StartCoroutine(aiSubmitAnimator.AnimateAiSubmit(card,
                    onArrived: () => submissionZone.ShowAiCard(card)));
            }
            else
            {
                // 폴백: 애니메이터 없음 → 즉시 표시.
                submissionZone.ShowAiCard(card);
            }
        }

        private void OnRoundResolved(RoundOutcome outcome)
        {
            // 양쪽 손패 재렌더링 (패자 카드 이동 반영).
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

            // 게임 로직 호출 BEFORE에 드래그 입력 차단 — AI 카드를 잡을 수 없도록.
            if (dragController != null)
            {
                dragController.SetAllInteractable(false);
            }

            _animatingSubmission = card;

            // 시각 슬라이드 시작.
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
