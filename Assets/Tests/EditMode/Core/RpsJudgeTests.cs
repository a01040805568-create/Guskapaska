using Guskapaska.Core;
using NUnit.Framework;
 
namespace Guskapaska.Tests.EditMode.Core
{
    /// <summary>
    /// <see cref="RpsJudge.Compare"/>의 9가지 (3×3) 모든 조합 검증.
    /// </summary>
    [TestFixture]
    public class RpsJudgeTests
    {
        // -------- 왼쪽 승 --------
 
        [Test]
        public void Scissors_Vs_Paper_LeftWins()
        {
            // 가위는 보자기를 이긴다
            Assert.AreEqual(RpsResult.LeftWins,
                RpsJudge.Compare(CardShape.Scissors, CardShape.Paper));
        }
 
        [Test]
        public void Paper_Vs_Rock_LeftWins()
        {
            // 보자기는 바위를 이긴다
            Assert.AreEqual(RpsResult.LeftWins,
                RpsJudge.Compare(CardShape.Paper, CardShape.Rock));
        }
 
        [Test]
        public void Rock_Vs_Scissors_LeftWins()
        {
            // 바위는 가위를 이긴다
            Assert.AreEqual(RpsResult.LeftWins,
                RpsJudge.Compare(CardShape.Rock, CardShape.Scissors));
        }
 
        // -------- 오른쪽 승 (위의 대칭) --------
 
        [Test]
        public void Paper_Vs_Scissors_RightWins()
        {
            Assert.AreEqual(RpsResult.RightWins,
                RpsJudge.Compare(CardShape.Paper, CardShape.Scissors));
        }
 
        [Test]
        public void Rock_Vs_Paper_RightWins()
        {
            Assert.AreEqual(RpsResult.RightWins,
                RpsJudge.Compare(CardShape.Rock, CardShape.Paper));
        }
 
        [Test]
        public void Scissors_Vs_Rock_RightWins()
        {
            Assert.AreEqual(RpsResult.RightWins,
                RpsJudge.Compare(CardShape.Scissors, CardShape.Rock));
        }
 
        // -------- 무승부 (같은 모양 3가지) --------
 
        [TestCase(CardShape.Scissors)]
        [TestCase(CardShape.Rock)]
        [TestCase(CardShape.Paper)]
        public void SameShape_Draw(CardShape shape)
        {
            // 같은 모양은 항상 무승부
            Assert.AreEqual(RpsResult.Draw, RpsJudge.Compare(shape, shape));
        }
    }
}