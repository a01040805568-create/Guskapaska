using System.Collections.Generic;
using System.Linq;
using Guskapaska.Core;
using NUnit.Framework;

namespace Guskapaska.Tests.EditMode.Core
{
    /// <summary>
    /// 00_GameDesign.md §2에 정의된 표준 12장 카드 구성을 검증.
    /// </summary>
    [TestFixture]
    public class CardFactoryTests
    {
        private List<Card> _set;

        [SetUp]
        public void SetUp()
        {
            // 각 테스트 시작 전 새 표준 셋을 만든다
            _set = CardFactory.CreateStandardSet();
        }

        [Test]
        public void CreateStandardSet_HasExactly12Cards()
        {
            // 카드는 정확히 12장
            Assert.AreEqual(12, _set.Count);
        }

        [Test]
        public void CreateStandardSet_TotalCoinValueIs13()
        {
            // 전체 카드의 코인 값 합은 13
            int totalCoins = _set.Sum(c => c.CoinValue);
            Assert.AreEqual(13, totalCoins);
        }

        [Test]
        public void CreateStandardSet_ShapeCounts_Are6_3_3()
        {
            // 모양별 장수: 가위 6장, 바위 3장, 보자기 3장
            int scissors = _set.Count(c => c.Shape == CardShape.Scissors);
            int rock = _set.Count(c => c.Shape == CardShape.Rock);
            int paper = _set.Count(c => c.Shape == CardShape.Paper);

            Assert.AreEqual(6, scissors, "가위 카드 수");
            Assert.AreEqual(3, rock, "바위 카드 수");
            Assert.AreEqual(3, paper, "보자기 카드 수");
        }

        [Test]
        public void CreateStandardSet_ScissorsCoinDistribution_Is_1_3_2()
        {
            // 가위 내부 코인 분포: 코인0 1장, 코인1 3장, 코인2 2장
            List<Card> scissors = _set.Where(c => c.Shape == CardShape.Scissors).ToList();

            int coin0 = scissors.Count(c => c.CoinValue == 0);
            int coin1 = scissors.Count(c => c.CoinValue == 1);
            int coin2 = scissors.Count(c => c.CoinValue == 2);

            Assert.AreEqual(1, coin0, "코인 0인 가위 수");
            Assert.AreEqual(3, coin1, "코인 1인 가위 수");
            Assert.AreEqual(2, coin2, "코인 2인 가위 수");
        }

        [Test]
        public void CreateStandardSet_AllIdsAreUnique()
        {
            // 모든 카드 Id는 고유해야 함
            int uniqueIdCount = _set.Select(c => c.Id).Distinct().Count();
            Assert.AreEqual(_set.Count, uniqueIdCount);
        }

        [Test]
        public void CreateStandardSet_ReturnsNewListEachCall()
        {
            // 팩토리는 매 호출마다 새 리스트를 반환해야 함
            // (호출자가 자유롭게 셔플/소유권을 가질 수 있도록)
            List<Card> a = CardFactory.CreateStandardSet();
            List<Card> b = CardFactory.CreateStandardSet();
            Assert.AreNotSame(a, b);
        }
    }
}