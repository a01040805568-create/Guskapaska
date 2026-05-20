using System.Collections;
using UnityEngine;
using Guskapaska.Core;

namespace Guskapaska.Game
{
    /// <summary>
    /// Owns the lifecycle of a single round: timer, AI pick, player submit,
    /// resolution, and outcome application to the GameState.
    /// See 00_GameDesign.md §5 for the round flow this implements.
    /// </summary>
    public class RoundController : MonoBehaviour
    {
        // 외부에서 주입받는 의존성
        private GameState _state;
        private GameEvents _events;
        private MatchConfig _config;
        private TimerController _timer;
        private AiRandomStrategy _aiStrategy;
        private System.Random _rng;

        // 라운드 진행 중 임시 상태
        private Card _pendingPlayerCard;   // 플레이어가 이번 라운드에 낸 카드 (확정 전)
        private Card _pendingAiCard;       // AI가 라운드 시작 시 미리 뽑아둔 카드
        private bool _playerSubmitted;     // 플레이어가 이번 라운드에 카드를 냈는지
        private bool _roundActive;         // 현재 라운드가 진행 중인지

        /// <summary>
        /// Inject dependencies. Must be called before StartRound.
        /// </summary>
        public void Initialize(
            GameState state,
            GameEvents events,
            MatchConfig config,
            TimerController timer,
            AiRandomStrategy ai,
            System.Random rng)
        {
            _state = state;
            _events = events;
            _config = config;
            _timer = timer;
            _aiStrategy = ai;
            _rng = rng;
        }

        /// <summary>
        /// Begins a new round. AI selects its card immediately (kept hidden),
        /// the timer starts, and the controller waits for SubmitPlayerCard
        /// or for the timer to expire.
        /// </summary>
        public void StartRound()
        {
            // 카드 보존 법칙 검증 (00_GameDesign.md §5.4)
            // 핸드 2개 + 무승부 스택의 합은 항상 10이어야 함. 위반은 에러 로그만 남기고 진행.
            int total = _state.PlayerHand.Count + _state.AiHand.Count + _state.DrawStash.Count;
            if (total != 10)
            {
                Debug.LogError(
                    $"[RoundController] Card conservation violated: total={total}, expected 10. " +
                    $"Player={_state.PlayerHand.Count}, Ai={_state.AiHand.Count}, Stash={_state.DrawStash.Count}");
            }

            // 라운드 번호 증가 및 시작 이벤트 발생
            _state.IncrementRound();
            _events.RaiseRoundStarted(_state.RoundNumber);

            // 임시 상태 초기화
            _pendingPlayerCard = null;
            _playerSubmitted = false;
            _roundActive = true;

            // AI는 라운드 시작 시 즉시 카드 선택 (아직 공개하지 않고 내부 저장)
            _pendingAiCard = _aiStrategy.SelectCard(_state.AiHand);

            // 타이머 시작 (만료되면 OnTimerExpired 호출)
            _timer.StartTimer(OnTimerExpired);
        }

        /// <summary>
        /// Called by external code (UI in later stages, tests in Stage 2)
        /// when the player picks a card. Ignored if not currently active or already submitted.
        /// </summary>
        public void SubmitPlayerCard(Card card)
        {
            if (!_roundActive || _playerSubmitted) return;
            if (card == null) return;

            // 플레이어 카드 확정
            _pendingPlayerCard = card;
            _playerSubmitted = true;

            // 타이머 정지 후 결과 해결 코루틴 시작
            _timer.StopTimer();
            StartCoroutine(ResolveRoundRoutine());
        }

        // 타이머가 0초에 도달했을 때 호출됨 → 무작위 카드 자동 제출
        private void OnTimerExpired()
        {
            if (!_roundActive || _playerSubmitted) return;

            // 플레이어가 시간 안에 못 냈으므로 무작위로 한 장 골라서 자동 제출
            _pendingPlayerCard = AutoSubmitPicker.PickRandom(_state.PlayerHand, _rng);
            _playerSubmitted = true;

            StartCoroutine(ResolveRoundRoutine());
        }

        // 라운드 판정 및 결과 적용 코루틴
        private IEnumerator ResolveRoundRoutine()
        {
            // 한 프레임 양보 (타이머 정지/이벤트 처리가 안정적으로 끝나도록)
            yield return null;

            // 양쪽 카드를 핸드에서 제거
            _state.PlayerHand.Remove(_pendingPlayerCard);
            _state.AiHand.Remove(_pendingAiCard);

            // 카드 제출 이벤트 발생 (UI/로거가 받아서 화면에 표시할 예정)
            _events.RaisePlayerCardSubmitted(_pendingPlayerCard);
            _events.RaiseAiCardSubmitted(_pendingAiCard);

            // 순수 로직으로 결과 계산 (Stage 1의 RoundResolver)
            RoundOutcome outcome = RoundResolver.Resolve(
                _pendingPlayerCard,
                _pendingAiCard,
                _state.DrawStash,
                _state.AccumulatedDrawCoins);

            // 결과를 GameState에 반영
            ApplyOutcome(outcome);

            // 라운드 결과/누적 코인/보석 변경 이벤트 발생
            _events.RaiseRoundResolved(outcome);
            _events.RaiseDrawAccumulatorChanged(_state.AccumulatedDrawCoins);
            _events.RaiseGemsChanged(_state.PlayerGems, _state.AiGems, _state.CenterGems);

            _roundActive = false;
        }

        // RoundOutcome을 GameState에 적용하는 로직
        // 규칙: 00_GameDesign.md §5.3
        private void ApplyOutcome(RoundOutcome outcome)
        {
            switch (outcome.Winner)
            {
                case RoundWinner.None:
                    // 무승부: 두 카드를 무승부 스택에 넣고, 코인 합계를 누적
                    _state.AddDrawAccumulator(
                        _pendingPlayerCard.CoinValue + _pendingAiCard.CoinValue,
                        new[] { _pendingPlayerCard, _pendingAiCard });
                    break;

                case RoundWinner.Player:
                    // 플레이어 승: 코인 획득, 양쪽 카드 + 누적 카드는 AI 핸드로
                    _state.AddPlayerGems(outcome.CoinsAwarded);
                    _state.AiHand.AddRange(outcome.CardsTransferredToLoser);
                    _state.ResetDrawAccumulator();
                    break;

                case RoundWinner.Ai:
                    // AI 승: 대칭. 카드들은 플레이어 핸드로
                    _state.AddAiGems(outcome.CoinsAwarded);
                    _state.PlayerHand.AddRange(outcome.CardsTransferredToLoser);
                    _state.ResetDrawAccumulator();
                    break;
            }
        }
    }
}