using UnityEngine;
using Guskapaska.Core;

namespace Guskapaska.Game
{
    /// <summary>
    /// Debug helper that subscribes to all GameEvents and prints them to the Console.
    /// This is how Stage 2 verifies the match flow without any UI.
    /// Logs are in English by design (developer-facing, not user-facing).
    /// </summary>
    public class GameLogger : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        // GameEvents는 GameManager.Awake에서 생성되므로 OnEnable 시점에 즉시 없을 수 있다.
        // → Start에서 구독을 시도하고, 구독 성공 여부를 보관해서 OnDisable에서 안전하게 해제.
        private bool _subscribed;

        private void Start()
        {
            if (gameManager == null)
            {
                Debug.LogError("[GameLogger] GameManager reference is missing.");
                return;
            }

            // GameManager.Awake가 먼저 돌아서 Events가 만들어진 상태여야 함
            // (실행 순서: Awake → Start. 같은 씬의 다른 객체끼리도 보장됨.)
            if (gameManager.Events == null)
            {
                Debug.LogError("[GameLogger] GameManager.Events is null at Start.");
                return;
            }

            Subscribe();
        }

        private void OnDisable()
        {
            // 안전하게 구독 해제
            if (_subscribed && gameManager != null && gameManager.Events != null)
            {
                Unsubscribe();
            }
        }

        // ----- 구독/해제 -----

        private void Subscribe()
        {
            var e = gameManager.Events;
            e.OnMatchStarted += LogMatchStarted;
            e.OnRoundStarted += LogRoundStarted;
            e.OnCountdownTriggered += LogCountdownTriggered;
            e.OnPlayerCardSubmitted += LogPlayerCardSubmitted;
            e.OnAiCardSubmitted += LogAiCardSubmitted;
            e.OnRoundResolved += LogRoundResolved;
            e.OnDrawAccumulatorChanged += LogDrawAccumulatorChanged;
            e.OnGemsChanged += LogGemsChanged;
            e.OnMatchEnded += LogMatchEnded;
            // OnTimerTick은 매 프레임 발생하므로 일부러 구독하지 않음 (로그 폭주 방지)
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            var e = gameManager.Events;
            e.OnMatchStarted -= LogMatchStarted;
            e.OnRoundStarted -= LogRoundStarted;
            e.OnCountdownTriggered -= LogCountdownTriggered;
            e.OnPlayerCardSubmitted -= LogPlayerCardSubmitted;
            e.OnAiCardSubmitted -= LogAiCardSubmitted;
            e.OnRoundResolved -= LogRoundResolved;
            e.OnDrawAccumulatorChanged -= LogDrawAccumulatorChanged;
            e.OnGemsChanged -= LogGemsChanged;
            e.OnMatchEnded -= LogMatchEnded;
            _subscribed = false;
        }

        // ----- 로그 핸들러 -----

        private void LogMatchStarted(GameState s)
        {
            Debug.Log($"[Match] Started. Player={s.PlayerGems}g, Ai={s.AiGems}g, Center={s.CenterGems}g");
        }

        private void LogRoundStarted(int n)
        {
            var s = gameManager.State;
            Debug.Log($"[Round {n}] Started. Player hand={s.PlayerHand.Count}, Ai hand={s.AiHand.Count}, Stash={s.DrawStash.Count}");
        }

        private void LogCountdownTriggered()
        {
            Debug.Log("[Timer] Countdown 3-2-1 triggered.");
        }

        private void LogPlayerCardSubmitted(Card c)
        {
            Debug.Log($"[Player] Submitted {c}");
        }

        private void LogAiCardSubmitted(Card c)
        {
            Debug.Log($"[AI] Submitted {c}");
        }

        private void LogRoundResolved(RoundOutcome o)
        {
            Debug.Log($"[Round {gameManager.State.RoundNumber}] Resolved. " +
                      $"Winner={o.Winner}, Coins={o.CoinsAwarded}, " +
                      $"DrawCoins {o.DrawCoinsBefore}→{o.DrawCoinsAfter}");
        }

        private void LogDrawAccumulatorChanged(int coins)
        {
            Debug.Log($"[Draw] Accumulator now {coins}");
        }

        private void LogGemsChanged(int player, int ai, int center)
        {
            Debug.Log($"[Gems] Player={player}, Ai={ai}, Center={center}");
        }

        private void LogMatchEnded(MatchResult r)
        {
            Debug.Log($"[Match] Ended. Winner={r.Winner}, Player={r.PlayerGems}g, Ai={r.AiGems}g, " +
                      $"Rounds={r.TotalRounds}, Reason={r.EndReason}");
        }
    }
}