using System;
using System.Collections.Generic;
using Guskapaska.Core;
using NUnit.Framework;

namespace Guskapaska.Tests.EditMode.Core
{
    /// <summary>
    /// <see cref="AutoSubmitPicker"/>의 결정성·예외 처리·RandomProvider 폴백 검증.
    /// </summary>
    [TestFixture]
    public class AutoSubmitPickerTests
    {
        [TearDown]
        public void TearDown()
        {
            // 정적 RandomProvider를 사용하는 테스트가 있으므로
            // 다른 테스트에 영향이 가지 않도록 매 테스트 후 리셋
            RandomProvider.Reset();
        }

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
        public void PickRandom_NullHand_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => AutoSubmitPicker.PickRandom(null, new System.Random(0)));
        }

        [Test]
        public void PickRandom_EmptyHand_Throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => AutoSubmitPicker.PickRandom(new Hand(), new System.Random(0)));
        }

        [Test]
        public void PickRandom_SeededRng_IsDeterministic()
        {
            // 동일한 시드의 RNG는 동일한 카드를 반환해야 한다
            Card a = AutoSubmitPicker.PickRandom(MakeFiveCardHand(), new System.Random(42));
            Card b = AutoSubmitPicker.PickRandom(MakeFiveCardHand(), new System.Random(42));

            Assert.AreEqual(a.Id, b.Id);
        }

        [Test]
        public void PickRandom_DoesNotModifyHand()
        {
            // 픽은 읽기 동작 — 핸드 카드 수가 변하면 안 됨
            Hand hand = MakeFiveCardHand();
            int countBefore = hand.Count;

            AutoSubmitPicker.PickRandom(hand, new System.Random(0));

            Assert.AreEqual(countBefore, hand.Count);
        }

        [Test]
        public void PickRandom_ReturnsCardFromHand()
        {
            // 반환된 카드는 반드시 핸드 내 카드여야 함
            Card picked = AutoSubmitPicker.PickRandom(MakeFiveCardHand(), new System.Random(12345));
            Hand hand = MakeFiveCardHand();
            Assert.IsTrue(hand.Contains(picked));
        }

        [Test]
        public void PickRandom_NullRng_UsesRandomProviderDefault()
        {
            // rng 인자를 생략하면 RandomProvider.Default를 사용해야 한다.
            // RandomProvider.Seed로 시드를 고정한 뒤 두 번 호출하면
            // 첫 호출과 동일한 시드를 다시 적용했을 때 같은 결과가 나와야 함.
            RandomProvider.Seed(7);
            Card first = AutoSubmitPicker.PickRandom(MakeFiveCardHand(), null);

            RandomProvider.Seed(7);
            Card second = AutoSubmitPicker.PickRandom(MakeFiveCardHand(), null);

            Assert.AreEqual(first.Id, second.Id);
        }

        [Test]
        public void PickRandom_OverManyIterations_CoversAllCards()
        {
            // 1000번 반복 시 5장 모두 한 번은 선택되어야 한다 (균등성 sanity 체크)
            Hand hand = MakeFiveCardHand();
            var rng = new System.Random(0);

            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < 1000; i++)
            {
                Card picked = AutoSubmitPicker.PickRandom(hand, rng);
                seen.Add(picked.Id);
            }

            Assert.AreEqual(5, seen.Count, "1000번의 자동 제출 중 5장 모두가 한 번 이상은 선택되어야 합니다.");
        }

        [Test]
        public void PickRandom_SingleCardHand_AlwaysReturnsThatCard()
        {
            // 한 장만 있을 때는 그 카드만 반환되어야 함
            var only = new Card("Card_S1_a", CardShape.Scissors, 1);
            var hand = new Hand(new[] { only });

            for (int i = 0; i < 50; i++)
            {
                Card picked = AutoSubmitPicker.PickRandom(hand, new System.Random(i));
                Assert.AreEqual(only.Id, picked.Id);
            }
        }
    }
}