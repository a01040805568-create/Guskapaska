using System;
using System.Collections;
using UnityEngine;

namespace Guskapaska.Game
{
    /// <summary>
    /// Per-round countdown timer. Drives OnTimerTick every frame,
    /// fires OnCountdownTriggered once when crossing the threshold,
    /// and invokes onExpired when reaching zero.
    /// </summary>
    public class TimerController : MonoBehaviour
    {
        /// <summary>True while a countdown coroutine is active.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Seconds left in the current round. 0 when not running.</summary>
        public float SecondsRemaining { get; private set; }

        // 외부에서 주입받는 의존성
        private GameEvents _events;
        private MatchConfig _config;

        // 현재 진행 중인 코루틴 핸들 (정지를 위해 보관)
        private Coroutine _routine;

        // 카운트다운 임계점을 한 번만 발생시키기 위한 플래그
        private bool _countdownTriggered;

        /// <summary>
        /// Inject dependencies. Must be called before StartTimer.
        /// </summary>
        public void Initialize(GameEvents events, MatchConfig config)
        {
            _events = events;
            _config = config;
        }

        /// <summary>
        /// Starts a fresh countdown. Invokes onExpired if the timer reaches zero
        /// without being stopped externally.
        /// </summary>
        public void StartTimer(Action onExpired)
        {
            // 이전 코루틴이 남아있으면 정리
            StopTimer();

            // 초기 상태 세팅
            SecondsRemaining = _config.RoundTimeSeconds;
            _countdownTriggered = false;
            IsRunning = true;

            _routine = StartCoroutine(TimerRoutine(onExpired));
        }

        /// <summary>
        /// Cancels the running coroutine cleanly. Safe to call anytime.
        /// </summary>
        public void StopTimer()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            IsRunning = false;
        }

        // 타이머 본체 코루틴
        private IEnumerator TimerRoutine(Action onExpired)
        {
            while (SecondsRemaining > 0f)
            {
                // 프레임당 경과 시간만큼 감소
                SecondsRemaining -= Time.deltaTime;
                if (SecondsRemaining < 0f) SecondsRemaining = 0f;

                // 매 프레임 OnTimerTick 발생
                _events.RaiseTimerTick(SecondsRemaining);

                // 임계점을 넘어가는 순간 단 한 번만 OnCountdownTriggered 발생
                if (!_countdownTriggered && SecondsRemaining <= _config.CountdownStartSeconds)
                {
                    _countdownTriggered = true;
                    _events.RaiseCountdownTriggered();
                }

                yield return null;
            }

            // 0초 도달 → 만료 처리
            IsRunning = false;
            _routine = null;
            onExpired?.Invoke();
        }
    }
}