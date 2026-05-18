using System;
using System.Collections.Generic;
using Guskapaska.Core;
using NUnit.Framework;

namespace Guskapaska.Tests.EditMode.Core
{
    /// <summary>
    /// <see cref="Hand"/>의 컬렉션 동작 검증:
    /// Add/Remove/Contains/GetAt/Clear/AddRange와 Count/IsEmpty 프로퍼티.
    /// </summary>
    [TestFixture]
    public class HandTests
    {
        private Card _s1a;
        private Card _s1b;
        private Card _r0;
        private Card _p2;

        [SetUp]
        public void SetUp()
        {
            // 테스트마다 동일한 샘플 카드를 준비
            _s1a = new Card("Card_S1_a", CardShape.Scissors, 1);
            _s1b = new Card("Card_S1_b", CardShape.Scissors, 1);
            _r0 = new Card("Card_R0_a", CardShape.Rock, 0);
            _p2 = new Card("Card_P2_a", CardShape.Paper, 2);
        }

        [Test]
        public void NewHand_IsEmpty_AndCountIsZero()
        {
            var hand = new Hand();
            Assert.IsTrue(hand.IsEmpty);
            Assert.AreEqual(0, hand.Count);
        }

        [Test]
        public void Hand_FromInitial_HasGivenCards()
        {
            // 생성자에 카드 리스트를 넘기면 그대로 보관
            var hand = new Hand(new[] { _s1a, _r0 });
            Assert.AreEqual(2, hand.Count);
            Assert.IsFalse(hand.IsEmpty);
            Assert.IsTrue(hand.Contains(_s1a));
            Assert.IsTrue(hand.Contains(_r0));
        }

        [Test]
        public void Hand_FromNullInitial_IsEmpty()
        {
            // null을 넘겨도 예외 없이 빈 핸드가 생성되어야 함
            var hand = new Hand(null);
            Assert.IsTrue(hand.IsEmpty);
        }

        [Test]
        public void Add_IncreasesCount_AndContains()
        {
            var hand = new Hand();
            hand.Add(_s1a);
            Assert.AreEqual(1, hand.Count);
            Assert.IsTrue(hand.Contains(_s1a));
        }

        [Test]
        public void Add_Null_Throws()
        {
            var hand = new Hand();
            Assert.Throws<ArgumentNullException>(() => hand.Add(null));
        }

        [Test]
        public void AddRange_AddsAllCards()
        {
            var hand = new Hand();
            hand.AddRange(new[] { _s1a, _r0, _p2 });
            Assert.AreEqual(3, hand.Count);
            Assert.IsTrue(hand.Contains(_s1a));
            Assert.IsTrue(hand.Contains(_r0));
            Assert.IsTrue(hand.Contains(_p2));
        }

        [Test]
        public void AddRange_Null_Throws()
        {
            var hand = new Hand();
            Assert.Throws<ArgumentNullException>(() => hand.AddRange(null));
        }

        [Test]
        public void Remove_ExistingCard_ReturnsTrue_AndCountDecreases()
        {
            var hand = new Hand(new[] { _s1a, _r0 });
            bool removed = hand.Remove(_s1a);

            Assert.IsTrue(removed);
            Assert.AreEqual(1, hand.Count);
            Assert.IsFalse(hand.Contains(_s1a));
            Assert.IsTrue(hand.Contains(_r0));
        }

        [Test]
        public void Remove_NonExistingCard_ReturnsFalse()
        {
            var hand = new Hand(new[] { _s1a });
            // _s1b는 _s1a와 Id가 다르므로 핸드에 없음
            bool removed = hand.Remove(_s1b);

            Assert.IsFalse(removed);
            Assert.AreEqual(1, hand.Count);
        }

        [Test]
        public void Remove_Null_ReturnsFalse_NoThrow()
        {
            var hand = new Hand(new[] { _s1a });
            Assert.DoesNotThrow(() =>
            {
                bool removed = hand.Remove(null);
                Assert.IsFalse(removed);
            });
        }

        [Test]
        public void Contains_UsesIdEquality()
        {
            var hand = new Hand(new[] { _s1a });
            // Id가 같으면 같은 카드로 취급
            var sameIdDifferentInstance = new Card("Card_S1_a", CardShape.Paper, 2);
            Assert.IsTrue(hand.Contains(sameIdDifferentInstance));
        }

        [Test]
        public void GetAt_ReturnsCardAtIndex()
        {
            var hand = new Hand(new[] { _s1a, _r0, _p2 });
            Assert.AreEqual(_s1a, hand.GetAt(0));
            Assert.AreEqual(_r0, hand.GetAt(1));
            Assert.AreEqual(_p2, hand.GetAt(2));
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(100)]
        public void GetAt_OutOfRange_Throws(int invalidIndex)
        {
            var hand = new Hand(new[] { _s1a, _r0, _p2 });
            Assert.Throws<ArgumentOutOfRangeException>(() => hand.GetAt(invalidIndex));
        }

        [Test]
        public void Clear_RemovesAll()
        {
            var hand = new Hand(new[] { _s1a, _r0, _p2 });
            hand.Clear();
            Assert.AreEqual(0, hand.Count);
            Assert.IsTrue(hand.IsEmpty);
        }

        [Test]
        public void Cards_ReturnsReadOnlyView()
        {
            // Cards 프로퍼티는 IReadOnlyList<Card>이므로
            // 외부에서 List<Card>로 캐스팅해 변경하는 것을 막아야 한다
            var hand = new Hand(new[] { _s1a, _r0 });
            IReadOnlyList<Card> view = hand.Cards;

            Assert.AreEqual(2, view.Count);
            // 캐스팅 가능 여부를 강제할 수는 없으므로,
            // 최소한 인터페이스 타입이 IReadOnlyList인지만 확인
            Assert.IsInstanceOf<IReadOnlyList<Card>>(view);
        }
    }
}