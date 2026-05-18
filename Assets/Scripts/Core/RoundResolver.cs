using System;
using System.Collections.Generic;
 
namespace Guskapaska.Core
{
    /// <summary>
    /// 라운드의 결과(<see cref="RoundOutcome"/>)를 계산하는 순수 함수 유틸.
    /// 핸드/덱 등의 상태는 변경하지 않는다 — 그 책임은 호출자(GameManager)에 있다.
    /// 게임 규칙은 00_GameDesign.md §5.3 참고.
    /// </summary>
    public static class RoundResolver
    {
        /// <summary>
        /// 양측이 낸 카드와 현재까지 누적된 무승부 정보를 받아 라운드 결과를 계산한다.
        /// </summary>
        /// <param name="playerCard">플레이어가 이번 라운드에 낸 카드.</param>
        /// <param name="aiCard">AI가 이번 라운드에 낸 카드.</param>
        /// <param name="accumulatedDrawCards">이전 무승부 라운드들에서 누적된 카드 (없으면 빈 리스트).</param>
        /// <param name="accumulatedDrawCoins">이전 무승부 라운드들에서 누적된 코인 합.</param>
        /// <returns>계산된 라운드 결과 객체.</returns>
        /// <exception cref="ArgumentNullException">카드 인자나 누적 카드 리스트가 null일 때.</exception>
        /// <exception cref="ArgumentOutOfRangeException">누적 코인이 음수일 때.</exception>
        public static RoundOutcome Resolve(
            Card playerCard,
            Card aiCard,
            IReadOnlyList<Card> accumulatedDrawCards,
            int accumulatedDrawCoins)
        {
            if (playerCard == null)
            {
                throw new ArgumentNullException(nameof(playerCard));
            }
            if (aiCard == null)
            {
                throw new ArgumentNullException(nameof(aiCard));
            }
            if (accumulatedDrawCards == null)
            {
                throw new ArgumentNullException(nameof(accumulatedDrawCards));
            }
            if (accumulatedDrawCoins < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(accumulatedDrawCoins),
                    accumulatedDrawCoins,
                    "누적 무승부 코인은 음수가 될 수 없습니다.");
            }
 
            RpsResult judgment = RpsJudge.Compare(playerCard.Shape, aiCard.Shape);
 
            // 무승부: 양 카드의 코인을 누적기에 더하고, 카드 이동은 없음
            if (judgment == RpsResult.Draw)
            {
                int newDrawCoins = accumulatedDrawCoins + playerCard.CoinValue + aiCard.CoinValue;
                return new RoundOutcome(
                    winner: RoundWinner.None,
                    playerCard: playerCard,
                    aiCard: aiCard,
                    coinsAwarded: 0,
                    drawCoinsBefore: accumulatedDrawCoins,
                    drawCoinsAfter: newDrawCoins,
                    cardsTransferredToLoser: Array.Empty<Card>());
            }
 
            // 결정적 승부: 승자는 양 카드 코인 + 누적 무승부 코인을 모두 가져감
            // 패자는 누적된 무승부 카드 전부 + 양쪽이 낸 카드 2장을 핸드로 받음
            int coinsAwarded = playerCard.CoinValue + aiCard.CoinValue + accumulatedDrawCoins;
 
            List<Card> transferred = new List<Card>(accumulatedDrawCards.Count + 2);
            transferred.AddRange(accumulatedDrawCards);
            transferred.Add(playerCard);
            transferred.Add(aiCard);
 
            RoundWinner winner = (judgment == RpsResult.LeftWins) ? RoundWinner.Player : RoundWinner.Ai;
 
            return new RoundOutcome(
                winner: winner,
                playerCard: playerCard,
                aiCard: aiCard,
                coinsAwarded: coinsAwarded,
                drawCoinsBefore: accumulatedDrawCoins,
                drawCoinsAfter: 0,
                cardsTransferredToLoser: transferred.AsReadOnly());
        }
    }
}