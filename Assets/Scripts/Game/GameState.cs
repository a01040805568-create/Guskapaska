using System.Collections.Generic;
using Guskapaska.Core;

namespace Guskapaska.Game
{
    /// <summary>
    /// Runtime state for a single match. Plain C# — not a MonoBehaviour.
    /// All mutations go through methods to keep state changes auditable.
    /// See 00_GameDesign.md §5 for the rules these methods implement.
    /// </summary>
    public class GameState
    {
        // 양 플레이어의 핸드 (Stage 1의 Hand 클래스 사용)
        public Hand PlayerHand { get; }
        public Hand AiHand { get; }

        // 무승부로 쌓인 카드 더미 (다음 결정적 라운드의 승자가 가져감)
        public List<Card> DrawStash { get; }

        // 중앙 보석 더미 (남은 보석 수)
        public int CenterGems { get; private set; }

        // 각 플레이어가 획득한 보석 수
        public int PlayerGems { get; private set; }
        public int AiGems { get; private set; }

        // 무승부가 누적되며 쌓인 코인 (다음 승자에게 더해짐)
        public int AccumulatedDrawCoins { get; private set; }

        // 현재 라운드 번호 (StartRound 호출 시 1부터 증가)
        public int RoundNumber { get; private set; }

        /// <summary>
        /// Creates a new game state with empty hands and the given starting gem count.
        /// </summary>
        public GameState(int totalGems)
        {
            PlayerHand = new Hand();
            AiHand = new Hand();
            DrawStash = new List<Card>();
            CenterGems = totalGems;
            PlayerGems = 0;
            AiGems = 0;
            AccumulatedDrawCoins = 0;
            RoundNumber = 0;
        }

        /// <summary>
        /// Adds gems to the player, pulling from the center pile. Clamped at 0.
        /// </summary>
        public void AddPlayerGems(int amount)
        {
            // 중앙에 남은 양보다 많이 줄 수 없으므로 clamp 처리
            int actual = (amount > CenterGems) ? CenterGems : amount;
            PlayerGems += actual;
            CenterGems -= actual;
            if (CenterGems < 0) CenterGems = 0;
        }

        /// <summary>
        /// Adds gems to the AI, pulling from the center pile. Clamped at 0.
        /// </summary>
        public void AddAiGems(int amount)
        {
            // AddPlayerGems와 동일한 clamp 로직
            int actual = (amount > CenterGems) ? CenterGems : amount;
            AiGems += actual;
            CenterGems -= actual;
            if (CenterGems < 0) CenterGems = 0;
        }

        /// <summary>
        /// Clears the draw stash and resets the accumulated coin counter to 0.
        /// Called when a decisive round ends a draw streak.
        /// </summary>
        public void ResetDrawAccumulator()
        {
            DrawStash.Clear();
            AccumulatedDrawCoins = 0;
        }

        /// <summary>
        /// Adds coins and cards to the draw accumulator. Called on draw rounds.
        /// </summary>
        public void AddDrawAccumulator(int coins, IEnumerable<Card> cards)
        {
            AccumulatedDrawCoins += coins;
            DrawStash.AddRange(cards);
        }

        /// <summary>Increments the round counter by one.</summary>
        public void IncrementRound()
        {
            RoundNumber++;
        }

        /// <summary>
        /// Match-end check. True if any of the end conditions in 00_GameDesign.md §7 is met.
        /// </summary>
        public bool IsGameOver()
        {
            // 종료 조건: 중앙 보석이 0이거나, 한쪽 핸드가 비었을 때
            return CenterGems == 0 || PlayerHand.IsEmpty || AiHand.IsEmpty;
        }

        /// <summary>
        /// Total cards across both hands plus the draw stash.
        /// Per 00_GameDesign.md §5.4, this must always equal 10 during play.
        /// </summary>
        public int TotalCardsInPlay()
        {
            return PlayerHand.Count + AiHand.Count + DrawStash.Count;
        }
    }
}