using System;
using Guskapaska.Core;
using NUnit.Framework;

namespace Guskapaska.Tests.EditMode.Core
{
    /// <summary>
    /// 불변 <see cref="Card"/> 값 객체에 대한 단위 테스트:
    /// 생성자 유효성 검증, 동등성/해시 동작, ToString 형식.
    /// </summary>
    [TestFixture]
    public class CardTests
    {
        [Test]
        public void Constructor_ValidArgs_SetsProperties()
        {
            // 정상 인자로 생성 시 모든 프로퍼티가 정확히 설정되는지 확인
            var c = new Card("Card_S1_a", CardShape.Scissors, 1);
            Assert.AreEqual("Card_S1_a", c.Id);
            Assert.AreEqual(CardShape.Scissors, c.Shape);
            Assert.AreEqual(1, c.CoinValue);
        }

        [Test]
        public void Constructor_NullId_Throws()
        {
            // Id가 null이면 ArgumentNullException 발생해야 함
            Assert.Throws<ArgumentNullException>(
                () => new Card(null, CardShape.Rock, 1));
        }

        [Test]
        public void Constructor_EmptyId_Throws()
        {
            // Id가 빈 문자열이면 ArgumentException 발생해야 함
            Assert.Throws<ArgumentException>(
                () => new Card(string.Empty, CardShape.Rock, 1));
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(100)]
        public void Constructor_CoinValueOutOfRange_Throws(int invalidCoin)
        {
            // 코인 값이 0~2 범위를 벗어나면 예외 발생해야 함
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Card("Card_X_a", CardShape.Rock, invalidCoin));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Constructor_CoinValueInRange_DoesNotThrow(int validCoin)
        {
            // 코인 값이 0, 1, 2면 예외 없이 정상 생성되어야 함
            Assert.DoesNotThrow(
                () => new Card("Card_X_a", CardShape.Rock, validCoin));
        }

        [Test]
        public void Equals_SameId_IsTrue_EvenIfShapeOrCoinDiffer()
        {
            // 동등성은 Id에만 기반 — Id가 같으면 모양/코인이 달라도 같은 카드
            var a = new Card("Card_S1_a", CardShape.Scissors, 1);
            var b = new Card("Card_S1_a", CardShape.Rock, 2);
            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void Equals_DifferentId_IsFalse()
        {
            // Id가 다르면 다른 카드
            var a = new Card("Card_S1_a", CardShape.Scissors, 1);
            var b = new Card("Card_S1_b", CardShape.Scissors, 1);
            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void Equals_NullOrOtherType_IsFalse()
        {
            // null이나 다른 타입과 비교하면 항상 false
            var a = new Card("Card_S1_a", CardShape.Scissors, 1);
            Assert.IsFalse(a.Equals(null));
            Assert.IsFalse(a.Equals("Card_S1_a"));
        }

        [Test]
        public void ToString_ContainsIdShapeAndCoin()
        {
            // ToString은 "Id(Shape,Coin)" 형식을 정확히 따라야 함
            var c = new Card("Card_S1_a", CardShape.Scissors, 1);
            string s = c.ToString();
            Assert.AreEqual("Card_S1_a(Scissors,1)", s);
        }
    }
}