using System;
using System.Collections.Generic;
using Guskapaska.Core;
using NUnit.Framework;

namespace Guskapaska.Tests.EditMode.Core
{
    /// <summary>
    /// <see cref="AiRandomStrategy"/>의 결정성·예외 처리·무작위성 검증.
    /// </summary>
    [TestFixture]
    public class AiRandomStrategyTests
    {
        /// <summary>5장의 서로 다른 카드로 구성된 표준 테스트 핸드.</summary>
        private static Hand MakeFiveCardHand()
        {
            return new Hand(new[]
            {
                new Card("Card_S0_a", CardShape.Scissors, 0),
                new Card("Card_S1_a", CardShape.Scissors, 1),
                new Card("Card_R0_a", CardShape.Rock, 0),
                new Card("Card_P1_a", CardShape.Paper, 1),
                new Card("Card_P2_a", CardShape.Paper, 2),
            });
        }

        [Test]
        public void SelectCard_NullHand_Throws()
        {
            var strategy = new AiRandomStrategy(new System.Random(0));
            Assert.Throws<ArgumentNullException>(() => strategy.SelectCard(null));
        }

        [Test]
        public void SelectCard_EmptyHand_Throws()
        {
            var strategy = new AiRandomStrategy(new System.Random(0));
            var emptyHand = new Hand();
            Assert.Throws<InvalidOperationException>(() => strategy.SelectCard(emptyHand));
        }

        [Test]
        public void SelectCard_SeededRng_IsDeterministic()
        {
            // 동일한 시드의 RNG라면 두 전략은 같은 핸드에 대해 같은 카드를 선택해야 한다
            var strategyA = new AiRandomStrategy(new System.Random(42));
            var strategyB = new AiRandomStrategy(new System.Random(42));

            Card a = strategyA.SelectCard(MakeFiveCardHand());
            Card b = strategyB.SelectCard(MakeFiveCardHand());

            Assert.AreEqual(a.Id, b.Id);
        }

        [Test]
        public void SelectCard_DoesNotModifyHand()
        {
            // 전략은 핸드를 읽기만 해야 한다 — 카드 제거는 호출자 책임
            var strategy = new AiRandomStrategy(new System.Random(0));
            Hand hand = MakeFiveCardHand();
            int countBefore = hand.Count;

            strategy.SelectCard(hand);

            Assert.AreEqual(countBefore, hand.Count);
        }

        [Test]
        public void SelectCard_ReturnsCardFromHand()
        {
            // 반환된 카드는 반드시 핸드 내 카드여야 함
            var strategy = new AiRandomStrategy(new System.Random(12345));
            Hand hand = MakeFiveCardHand();

            Card picked = strategy.SelectCard(hand);

            Assert.IsTrue(hand.Contains(picked));
        }

        [Test]
        public void SelectCard_OverManyIterations_CoversAllCards()
        {
            // 1000번 선택을 반복하면 5장의 카드가 모두 적어도 한 번씩은 뽑혀야 한다.
            // 시드를 고정해 테스트가 결정적이게 유지한다.
            var strategy = new AiRandomStrategy(new System.Random(0));
            Hand hand = MakeFiveCardHand();

            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < 1000; i++)
            {
                Card picked = strategy.SelectCard(hand);
                seen.Add(picked.Id);
            }

            Assert.AreEqual(5, seen.Count, "1000번의 시도에서 5장 모두가 한 번 이상은 선택되어야 합니다.");
        }

        [Test]
        public void SelectCard_SingleCardHand_AlwaysReturnsThatCard()
        {
            // 한 장만 있는 핸드는 항상 그 카드만 반환
            var only = new Card("Card_S1_a", CardShape.Scissors, 1);
            var hand = new Hand(new[] { only });
            var strategy = new AiRandomStrategy(new System.Random(0));

            for (int i = 0; i < 50; i++)
            {
                Card picked = strategy.SelectCard(hand);
                Assert.AreEqual(only.Id, picked.Id);
            }
        }
    }
}