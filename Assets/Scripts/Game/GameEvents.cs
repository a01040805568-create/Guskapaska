using System;
using System.Collections.Generic;
using Guskapaska.Core;

namespace Guskapaska.Game
{
    /// <summary>
    /// Event bus connecting the game manager/controllers to UI and debug subscribers.
    /// Uses plain C# events (no UnityEvent). Subscribers must unsubscribe in OnDisable/OnDestroy.
    /// </summary>
    public class GameEvents
    {
        // 매치 시작 시 1회 발생
        public event Action<GameState> OnMatchStarted;

        // 매 라운드 시작 시 발생 (인자: 라운드 번호)
        public event Action<int> OnRoundStarted;

        // 매 프레임 타이머 진행 중 발생 (인자: 남은 초)
        public event Action<float> OnTimerTick;

        // 타이머가 countdownStartSeconds 임계점을 넘어가는 순간 1회 발생
        public event Action OnCountdownTriggered;

        // 플레이어/AI 카드 제출 시 발생
        public event Action<Card> OnPlayerCardSubmitted;
        public event Action<Card> OnAiCardSubmitted;

        // 라운드 판정 완료 시 발생
        public event Action<RoundOutcome> OnRoundResolved;

        // 누적 무승부 코인 변경 시 발생 (인자: 현재 누적 코인)
        public event Action<int> OnDrawAccumulatorChanged;

        // 보석 수 변경 시 발생 (인자: 플레이어 보석, AI 보석, 중앙 보석)
        public event Action<int, int, int> OnGemsChanged;

        // 매치 종료 시 발생
        public event Action<MatchResult> OnMatchEnded;

        // ----- Raise 헬퍼 메서드 -----
        // GameManager / 컨트롤러에서만 호출해야 함. UI/디버그 코드는 호출 금지.

        public void RaiseMatchStarted(GameState s) => OnMatchStarted?.Invoke(s);
        public void RaiseRoundStarted(int n) => OnRoundStarted?.Invoke(n);
        public void RaiseTimerTick(float secondsRemaining) => OnTimerTick?.Invoke(secondsRemaining);
        public void RaiseCountdownTriggered() => OnCountdownTriggered?.Invoke();
        public void RaisePlayerCardSubmitted(Card c) => OnPlayerCardSubmitted?.Invoke(c);
        public void RaiseAiCardSubmitted(Card c) => OnAiCardSubmitted?.Invoke(c);
        public void RaiseRoundResolved(RoundOutcome o) => OnRoundResolved?.Invoke(o);
        public void RaiseDrawAccumulatorChanged(int coins) => OnDrawAccumulatorChanged?.Invoke(coins);
        public void RaiseGemsChanged(int player, int ai, int center) => OnGemsChanged?.Invoke(player, ai, center);
        public void RaiseMatchEnded(MatchResult r) => OnMatchEnded?.Invoke(r);
    }
}