using Guskapaska.Core;
using NUnit.Framework;

namespace Guskapaska.Tests.EditMode.Core
{
    /// <summary>
    /// <see cref="RandomProvider"/>의 시드/리셋 동작 검증.
    /// 정적 상태를 다루므로 각 테스트는 끝나고 반드시 <see cref="RandomProvider.Reset"/>으로 복원한다.
    /// </summary>
    [TestFixture]
    public class RandomProviderTests
    {
        [TearDown]
        public void TearDown()
        {
            // 정적 상태가 다른 테스트에 영향을 주지 않도록 매 테스트 후 리셋
            RandomProvider.Reset();
        }

        [Test]
        public void Default_IsNotNull_OnStartup()
        {
            // 정적 초기화로 Default는 항상 사용 가능해야 함
            Assert.IsNotNull(RandomProvider.Default);
        }

        [Test]
        public void Seed_SameValue_ProducesSameSequence()
        {
            // 동일한 시드로 두 번 설정하면 첫 N개 난수가 정확히 일치해야 함
            RandomProvider.Seed(123);
            int[] first = new int[5];
            for (int i = 0; i < first.Length; i++)
            {
                first[i] = RandomProvider.Default.Next();
            }

            RandomProvider.Seed(123);
            int[] second = new int[5];
            for (int i = 0; i < second.Length; i++)
            {
                second[i] = RandomProvider.Default.Next();
            }

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void Seed_DifferentValues_ProduceDifferentSequences()
        {
            // 다른 시드는 (거의 확실히) 다른 시퀀스를 생성
            RandomProvider.Seed(1);
            int a = RandomProvider.Default.Next();

            RandomProvider.Seed(2);
            int b = RandomProvider.Default.Next();

            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void Reset_ReplacesDefaultInstance()
        {
            // Reset 후 Default는 새로 만들어진 인스턴스여야 함
            RandomProvider.Seed(42);
            System.Random before = RandomProvider.Default;

            RandomProvider.Reset();
            System.Random after = RandomProvider.Default;

            Assert.AreNotSame(before, after);
        }
    }
}