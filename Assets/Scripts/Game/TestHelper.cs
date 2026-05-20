#if UNITY_INCLUDE_TESTS

using System.Collections.Generic;
using Guskapaska.Core;

namespace Guskapaska.Game
{
    /// <summary>
    /// Test-only helpers for injecting deterministic state into a running match.
    /// Excluded from non-test builds via UNITY_INCLUDE_TESTS guard.
    /// </summary>
    public static class TestHelper
    {
        /// <summary>
        /// Replaces the player's hand with the given cards. The previous hand is cleared.
        /// </summary>
        public static void OverridePlayerHand(GameState state, IEnumerable<Card> cards)
        {
            state.PlayerHand.Clear();
            state.PlayerHand.AddRange(cards);
        }

        /// <summary>
        /// Replaces the AI's hand with the given cards. The previous hand is cleared.
        /// </summary>
        public static void OverrideAiHand(GameState state, IEnumerable<Card> cards)
        {
            state.AiHand.Clear();
            state.AiHand.AddRange(cards);
        }

        /// <summary>
        /// Replaces both hands at once. Each list should sum to keep the
        /// card-conservation invariant (total cards in play = 10).
        /// </summary>
        public static void OverrideBothHands(GameState state, IEnumerable<Card> playerCards, IEnumerable<Card> aiCards)
        {
            OverridePlayerHand(state, playerCards);
            OverrideAiHand(state, aiCards);
        }
    }
}

#endif