using System;
using System.Collections.Generic;
using System.Linq;
using Guskapaska.Core;
using NUnit.Framework;

namespace Guskapaska.Tests.EditMode.Core
{
    /// <summary>
    /// <see cref="Deck"/>의 셔플 결정성, 카드 뽑기 동작, 경계/예외 처리 검증.
    /// </summary>
    [TestFixture]
    public class DeckTests
    {
        /// <summary>
        /// 표준 12장 셋의 깊은 복사본을 만든다.
        /// 각 테스트가 독립적으로 작동하도록 매번 새 인스턴스를 사용한다.
        /// </summary>
        private static List<Card> StandardSet()
        {
            return CardFactory.CreateStandardSet();
        }

        [Test]
        public void Constructor_NullCards_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Deck(null));
        }

        [Test]
        public void NewDeck_CountMatchesInput()
        {
            var deck = new Deck(StandardSet());
            Assert.AreEqual(12, deck.Count);
            Assert.IsFalse(deck.IsEmpty);
        }

        [Test]
        public void Shuffle_SameSeed_ProducesSameOrder()
        {
            // 동일한 시드의 RNG로 셔플하면 두 덱의 순서가 정확히 일치해야 함 (재현성)
            var deckA = new Deck(StandardSet());
            var deckB = new Deck(StandardSet());

            deckA.Shuffle(new System.Random(42));
            deckB.Shuffle(new System.Random(42));

            List<string> idsA = deckA.Peek().Select(c => c.Id).ToList();
            List<string> idsB = deckB.Peek().Select(c => c.Id).ToList();

            CollectionAssert.AreEqual(idsA, idsB);
        }

        [Test]
        public void Shuffle_DifferentSeeds_ProduceDifferentOrders()
        {
            // 다른 시드라면 두 셔플 결과는 거의 항상 달라야 한다 (12!은 매우 큰 수).
            var deckA = new Deck(StandardSet());
            var deckB = new Deck(StandardSet());

            deckA.Shuffle(new System.Random(1));
            deckB.Shuffle(new System.Random(99999));

            List<string> idsA = deckA.Peek().Select(c => c.Id).ToList();
            List<string> idsB = deckB.Peek().Select(c => c.Id).ToList();

            CollectionAssert.AreNotEqual(idsA, idsB);
        }

        [Test]
        public void Shuffle_PreservesAllCards()
        {
            // 셔플은 순서만 바꿀 뿐, 카드 집합 자체는 동일해야 한다
            var deck = new Deck(StandardSet());
            HashSet<string> before = deck.Peek().Select(c => c.Id).ToHashSet();

            deck.Shuffle(new System.Random(123));
            HashSet<string> after = deck.Peek().Select(c => c.Id).ToHashSet();

            Assert.AreEqual(12, deck.Count);
            CollectionAssert.AreEquivalent(before, after);
        }

        [Test]
        public void Shuffle_NullRng_DoesNotThrow_AndKeepsCardCount()
        {
            // rng가 null이면 내부적으로 새 인스턴스 생성 — 예외 없이 동작해야 함
            var deck = new Deck(StandardSet());
            Assert.DoesNotThrow(() => deck.Shuffle(null));
            Assert.AreEqual(12, deck.Count);
        }

        [Test]
        public void DrawTop_ReducesCountByOne_AndReturnsTopCard()
        {
            var initial = StandardSet();
            var deck = new Deck(initial);

            Card top = deck.DrawTop();

            Assert.AreEqual(11, deck.Count);
            // 셔플하지 않았으므로 맨 위는 초기 리스트의 첫 카드여야 함
            Assert.AreEqual(initial[0].Id, top.Id);
        }

        [Test]
        public void DrawTop_FromEmpty_Throws()
        {
            var deck = new Deck(new List<Card>());
            Assert.Throws<InvalidOperationException>(() => deck.DrawTop());
        }

        [Test]
        public void DrawTopN_ReturnsRequestedCount_InTopOrder()
        {
            var initial = StandardSet();
            var deck = new Deck(initial);

            List<Card> drawn = deck.DrawTop(5);

            Assert.AreEqual(5, drawn.Count);
            Assert.AreEqual(7, deck.Count);
            // 뽑힌 5장이 원래 덱의 0~4 인덱스와 순서까지 동일해야 함
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(initial[i].Id, drawn[i].Id);
            }
        }

        [Test]
        public void DrawTopN_Zero_ReturnsEmptyList_AndDeckUnchanged()
        {
            var deck = new Deck(StandardSet());
            List<Card> drawn = deck.DrawTop(0);

            Assert.AreEqual(0, drawn.Count);
            Assert.AreEqual(12, deck.Count);
        }

        [Test]
        public void DrawTopN_NegativeCount_Throws()
        {
            var deck = new Deck(StandardSet());
            Assert.Throws<ArgumentOutOfRangeException>(() => deck.DrawTop(-1));
        }

        [Test]
        public void DrawTopN_MoreThanAvailable_Throws()
        {
            var deck = new Deck(StandardSet());
            Assert.Throws<InvalidOperationException>(() => deck.DrawTop(13));
        }

        [Test]
        public void Peek_DoesNotModifyDeck()
        {
            var deck = new Deck(StandardSet());
            int countBefore = deck.Count;

            var view = deck.Peek();

            Assert.AreEqual(countBefore, deck.Count);
            Assert.AreEqual(countBefore, view.Count);
        }

        [Test]
        public void DrawAll_LeavesDeckEmpty()
        {
            var deck = new Deck(StandardSet());
            deck.DrawTop(12);

            Assert.AreEqual(0, deck.Count);
            Assert.IsTrue(deck.IsEmpty);
        }
    }
}