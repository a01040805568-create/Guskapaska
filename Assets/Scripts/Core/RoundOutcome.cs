using System;
using System.Collections.Generic;
 
namespace Guskapaska.Core
{
    /// <summary>
    /// 한 라운드의 승자.
    /// </summary>
    public enum RoundWinner
    {
        /// <summary>무승부.</summary>
        None,
        /// <summary>플레이어 승.</summary>
        Player,
        /// <summary>AI 승.</summary>
        Ai
    }
 
    /// <summary>
    /// 한 라운드의 결과를 기술하는 불변 값 객체.
    /// <see cref="RoundResolver"/>가 생성하며,
    /// Stage 2의 GameManager가 이 값을 기반으로 핸드/보석 상태를 갱신하고
    /// UI에 애니메이션 신호를 보낸다.
    /// 라운드 처리 규칙은 00_GameDesign.md §5.3 참고.
    /// </summary>
    public class RoundOutcome
    {
        /// <summary>이번 라운드의 승자 (또는 무승부).</summary>
        public RoundWinner Winner { get; }
 
        /// <summary>플레이어가 이번 라운드에 낸 카드.</summary>
        public Card PlayerCard { get; }
 
        /// <summary>AI가 이번 라운드에 낸 카드.</summary>
        public Card AiCard { get; }
 
        /// <summary>
        /// 승자가 가져가는 보석 수.
        /// 무승부면 0.
        /// 승리 시 = (자기 카드 코인) + (상대 카드 코인) + (이전까지 누적된 무승부 코인).
        /// </summary>
        public int CoinsAwarded { get; }
 
        /// <summary>이번 라운드 진입 시점의 누적 무승부 코인 값.</summary>
        public int DrawCoinsBefore { get; }
 
        /// <summary>이번 라운드 종료 시점의 누적 무승부 코인 값. 결정적 승부면 0으로 리셋된다.</summary>
        public int DrawCoinsAfter { get; }
 
        /// <summary>
        /// 패자의 핸드로 이동해야 할 카드들.
        /// 무승부면 빈 리스트.
        /// 결정적 승부면 = (이전까지 누적된 무승부 카드들) + (양쪽이 이번 라운드에 낸 카드 2장).
        /// </summary>
        public IReadOnlyList<Card> CardsTransferredToLoser { get; }
 
        /// <summary>
        /// 라운드 결과 객체를 생성한다. 일반적으로 <see cref="RoundResolver.Resolve"/>가 호출하지만,
        /// 테스트에서 직접 만들 수도 있다.
        /// </summary>
        /// <exception cref="ArgumentNullException">필수 인자가 null일 때 발생.</exception>
        public RoundOutcome(
            RoundWinner winner,
            Card playerCard,
            Card aiCard,
            int coinsAwarded,
            int drawCoinsBefore,
            int drawCoinsAfter,
            IReadOnlyList<Card> cardsTransferredToLoser)
        {
            if (playerCard == null)
            {
                throw new ArgumentNullException(nameof(playerCard));
            }
            if (aiCard == null)
            {
                throw new ArgumentNullException(nameof(aiCard));
            }
            if (cardsTransferredToLoser == null)
            {
                throw new ArgumentNullException(nameof(cardsTransferredToLoser));
            }
 
            Winner = winner;
            PlayerCard = playerCard;
            AiCard = aiCard;
            CoinsAwarded = coinsAwarded;
            DrawCoinsBefore = drawCoinsBefore;
            DrawCoinsAfter = drawCoinsAfter;
            CardsTransferredToLoser = cardsTransferredToLoser;
        }
    }
}