using System;
using System.Collections.Generic;
using System.Linq;
using Guskapaska.Core;
using NUnit.Framework;
 
namespace Guskapaska.Tests.EditMode.Core
{
    /// <summary>
    /// <see cref="RoundResolver.Resolve"/>의 모든 경우 검증.
    /// 00_GameDesign.md §8의 예시 시나리오를 그대로 재현한다.
    /// </summary>
    [TestFixture]
    public class RoundResolverTests
    {
        // 자주 쓰는 헬퍼: 카드 생성을 짧게
        private static Card MakeCard(string id, CardShape shape, int coin)
        {
            return new Card(id, shape, coin);
        }
 
        // -------- 시나리오 1: 단순 승, 누적 무승부 없음 --------
 
        [Test]
        public void Simple_PlayerWin_NoDrawPot()
        {
            // 플레이어 가위-1 vs AI 보자기-2 → 플레이어가 3코인 획득
            var p = MakeCard("Card_S1_a", CardShape.Scissors, 1);
            var a = MakeCard("Card_P2_a", CardShape.Paper, 2);
 
            RoundOutcome outcome = RoundResolver.Resolve(p, a, Array.Empty<Card>(), 0);
 
            Assert.AreEqual(RoundWinner.Player, outcome.Winner);
            Assert.AreEqual(3, outcome.CoinsAwarded);
            Assert.AreEqual(0, outcome.DrawCoinsBefore);
            Assert.AreEqual(0, outcome.DrawCoinsAfter);
 
            // 두 카드 모두 패자(AI) 핸드로 이동
            Assert.AreEqual(2, outcome.CardsTransferredToLoser.Count);
            CollectionAssert.AreEquivalent(
                new[] { p.Id, a.Id },
                outcome.CardsTransferredToLoser.Select(c => c.Id).ToList());
        }
 
        // -------- 시나리오 2: 0코인끼리 승 — 카드는 이동, 코인 보상은 0 --------
 
        [Test]
        public void ZeroCoin_Win_NoCoinsButCardsStillTransfer()
        {
            // 플레이어 가위-0 vs AI 보자기-0 → 0코인이지만 카드 2장은 AI 핸드로 이동
            var p = MakeCard("Card_S0_a", CardShape.Scissors, 0);
            var a = MakeCard("Card_P0_a", CardShape.Paper, 0);
 
            RoundOutcome outcome = RoundResolver.Resolve(p, a, Array.Empty<Card>(), 0);
 
            Assert.AreEqual(RoundWinner.Player, outcome.Winner);
            Assert.AreEqual(0, outcome.CoinsAwarded);
            Assert.AreEqual(2, outcome.CardsTransferredToLoser.Count);
        }
 
        // -------- 시나리오 3: 누적 무승부 후 결정적 승부 --------
 
        [Test]
        public void Win_After_AccumulatedDraws_TransfersAllCardsAndCoins()
        {
            // 무승부로 4코인 + 4장이 쌓인 상태에서 플레이어 가위-1 vs AI 보자기-2 승리
            // 보상 = 1 + 2 + 4 = 7 코인, 이동 카드 = 4(누적) + 2(이번) = 6장
            var stash = new List<Card>
            {
                MakeCard("Card_R0_a", CardShape.Rock, 0),
                MakeCard("Card_R1_a", CardShape.Rock, 1),
                MakeCard("Card_R2_a", CardShape.Rock, 2),
                MakeCard("Card_S0_a", CardShape.Scissors, 0),
            };
            var p = MakeCard("Card_S1_a", CardShape.Scissors, 1);
            var a = MakeCard("Card_P2_a", CardShape.Paper, 2);
 
            RoundOutcome outcome = RoundResolver.Resolve(p, a, stash, 4);
 
            Assert.AreEqual(RoundWinner.Player, outcome.Winner);
            Assert.AreEqual(7, outcome.CoinsAwarded);
            Assert.AreEqual(4, outcome.DrawCoinsBefore);
            Assert.AreEqual(0, outcome.DrawCoinsAfter);
            Assert.AreEqual(6, outcome.CardsTransferredToLoser.Count);
 
            // 누적 카드 4장 + 이번 라운드 2장이 모두 포함되어야 함
            var ids = outcome.CardsTransferredToLoser.Select(c => c.Id).ToList();
            CollectionAssert.AreEquivalent(
                new[] { "Card_R0_a", "Card_R1_a", "Card_R2_a", "Card_S0_a", "Card_S1_a", "Card_P2_a" },
                ids);
        }
 
        // -------- 시나리오 4: 무승부는 누적기에 더하고 카드는 안 옮긴다 --------
 
        [Test]
        public void Draw_AccumulatesCoins_NoCardTransfer()
        {
            // 진입 시 누적 4 + 이번 라운드 두 카드 합 3 = 7로 누적
            var p = MakeCard("Card_R2_a", CardShape.Rock, 2);
            var a = MakeCard("Card_R1_a", CardShape.Rock, 1);
 
            RoundOutcome outcome = RoundResolver.Resolve(p, a, new List<Card>(), 4);
 
            Assert.AreEqual(RoundWinner.None, outcome.Winner);
            Assert.AreEqual(0, outcome.CoinsAwarded);
            Assert.AreEqual(4, outcome.DrawCoinsBefore);
            Assert.AreEqual(7, outcome.DrawCoinsAfter);
            Assert.AreEqual(0, outcome.CardsTransferredToLoser.Count);
        }
 
        // -------- 시나리오 5: AI 승 (대칭성) --------
 
        [Test]
        public void AiWins_Symmetric()
        {
            // 플레이어 바위-0 vs AI 보자기-2 → AI가 2코인 획득
            var p = MakeCard("Card_R0_a", CardShape.Rock, 0);
            var a = MakeCard("Card_P2_a", CardShape.Paper, 2);
 
            RoundOutcome outcome = RoundResolver.Resolve(p, a, Array.Empty<Card>(), 0);
 
            Assert.AreEqual(RoundWinner.Ai, outcome.Winner);
            Assert.AreEqual(2, outcome.CoinsAwarded);
            Assert.AreEqual(0, outcome.DrawCoinsAfter);
            Assert.AreEqual(2, outcome.CardsTransferredToLoser.Count);
        }
 
        // -------- 보너스: 첫 라운드가 무승부일 때 누적기가 0에서 시작 --------
 
        [Test]
        public void FirstRoundDraw_FromZeroPot()
        {
            // 누적이 0인 상태에서 무승부 → 누적기 = 1+1 = 2
            var p = MakeCard("Card_S1_a", CardShape.Scissors, 1);
            var a = MakeCard("Card_S1_b", CardShape.Scissors, 1);
 
            RoundOutcome outcome = RoundResolver.Resolve(p, a, Array.Empty<Card>(), 0);
 
            Assert.AreEqual(RoundWinner.None, outcome.Winner);
            Assert.AreEqual(0, outcome.DrawCoinsBefore);
            Assert.AreEqual(2, outcome.DrawCoinsAfter);
        }
 
        // -------- 보너스: 누적 카드 순서 보존 확인 --------
 
        [Test]
        public void Win_PreservesAccumulatedCardOrder_ThenAppendsPlayedCards()
        {
            // 누적 카드는 들어온 순서대로, 마지막에 (플레이어, AI) 카드가 차례로 붙어야 한다
            var c1 = MakeCard("Card_R0_a", CardShape.Rock, 0);
            var c2 = MakeCard("Card_P0_a", CardShape.Paper, 0);
            var p = MakeCard("Card_S1_a", CardShape.Scissors, 1);
            var a = MakeCard("Card_P1_a", CardShape.Paper, 1);
 
            RoundOutcome outcome = RoundResolver.Resolve(
                p, a, new List<Card> { c1, c2 }, 0);
 
            var ids = outcome.CardsTransferredToLoser.Select(c => c.Id).ToList();
            Assert.AreEqual("Card_R0_a", ids[0]);
            Assert.AreEqual("Card_P0_a", ids[1]);
            Assert.AreEqual("Card_S1_a", ids[2]);
            Assert.AreEqual("Card_P1_a", ids[3]);
        }
 
        // -------- 인자 검증 --------
 
        [Test]
        public void Resolve_NullPlayerCard_Throws()
        {
            var a = MakeCard("Card_P2_a", CardShape.Paper, 2);
            Assert.Throws<ArgumentNullException>(() =>
                RoundResolver.Resolve(null, a, Array.Empty<Card>(), 0));
        }
 
        [Test]
        public void Resolve_NullAiCard_Throws()
        {
            var p = MakeCard("Card_S1_a", CardShape.Scissors, 1);
            Assert.Throws<ArgumentNullException>(() =>
                RoundResolver.Resolve(p, null, Array.Empty<Card>(), 0));
        }
 
        [Test]
        public void Resolve_NullAccumulatedDrawCards_Throws()
        {
            var p = MakeCard("Card_S1_a", CardShape.Scissors, 1);
            var a = MakeCard("Card_P2_a", CardShape.Paper, 2);
            Assert.Throws<ArgumentNullException>(() =>
                RoundResolver.Resolve(p, a, null, 0));
        }
 
        [Test]
        public void Resolve_NegativeDrawCoins_Throws()
        {
            var p = MakeCard("Card_S1_a", CardShape.Scissors, 1);
            var a = MakeCard("Card_P2_a", CardShape.Paper, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RoundResolver.Resolve(p, a, Array.Empty<Card>(), -1));
        }
    }
}