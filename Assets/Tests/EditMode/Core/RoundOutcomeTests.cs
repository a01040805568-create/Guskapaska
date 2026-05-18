using System;
using System.Collections.Generic;
using Guskapaska.Core;
using NUnit.Framework;
 
namespace Guskapaska.Tests.EditMode.Core
{
    /// <summary>
    /// 불변 값 객체 <see cref="RoundOutcome"/>의 생성자 검증 및 프로퍼티 보존 확인.
    /// </summary>
    [TestFixture]
    public class RoundOutcomeTests
    {
        private Card _playerCard;
        private Card _aiCard;
 
        [SetUp]
        public void SetUp()
        {
            _playerCard = new Card("Card_S1_a", CardShape.Scissors, 1);
            _aiCard = new Card("Card_P2_a", CardShape.Paper, 2);
        }
 
        [Test]
        public void Constructor_ValidArgs_PropertiesPreserved()
        {
            // 전달한 모든 인자가 그대로 프로퍼티에 보존되는지 확인
            IReadOnlyList<Card> transferred = new List<Card> { _playerCard, _aiCard }.AsReadOnly();
 
            var outcome = new RoundOutcome(
                winner: RoundWinner.Player,
                playerCard: _playerCard,
                aiCard: _aiCard,
                coinsAwarded: 3,
                drawCoinsBefore: 0,
                drawCoinsAfter: 0,
                cardsTransferredToLoser: transferred);
 
            Assert.AreEqual(RoundWinner.Player, outcome.Winner);
            Assert.AreEqual(_playerCard, outcome.PlayerCard);
            Assert.AreEqual(_aiCard, outcome.AiCard);
            Assert.AreEqual(3, outcome.CoinsAwarded);
            Assert.AreEqual(0, outcome.DrawCoinsBefore);
            Assert.AreEqual(0, outcome.DrawCoinsAfter);
            Assert.AreSame(transferred, outcome.CardsTransferredToLoser);
        }
 
        [Test]
        public void Constructor_NullPlayerCard_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new RoundOutcome(
                    RoundWinner.None, null, _aiCard, 0, 0, 0,
                    Array.Empty<Card>()));
        }
 
        [Test]
        public void Constructor_NullAiCard_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new RoundOutcome(
                    RoundWinner.None, _playerCard, null, 0, 0, 0,
                    Array.Empty<Card>()));
        }
 
        [Test]
        public void Constructor_NullTransferList_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new RoundOutcome(
                    RoundWinner.None, _playerCard, _aiCard, 0, 0, 0,
                    null));
        }
 
        [Test]
        public void Constructor_DrawOutcome_EmptyTransferIsAllowed()
        {
            // 무승부일 때는 이동 카드가 없어야 하지만, 그건 RoundResolver가 보장하는 것이며
            // RoundOutcome 자체는 단순히 값 보관 객체이므로 빈 리스트도 정상 허용
            Assert.DoesNotThrow(() =>
                new RoundOutcome(
                    RoundWinner.None, _playerCard, _aiCard, 0, 0, 0,
                    Array.Empty<Card>()));
        }
    }
}