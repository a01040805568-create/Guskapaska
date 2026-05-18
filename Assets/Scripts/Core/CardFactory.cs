using System.Collections.Generic;

namespace Guskapaska.Core
{
    /// <summary>
    /// 00_GameDesign.md §2에 정의된 표준 12장 카드 셋을 생성하는 팩토리.
    /// </summary>
    public static class CardFactory
    {
        /// <summary>
        /// 00_GameDesign.md §2에 정의된 표준 12장 카드 셋을 생성한다.
        /// 카드는 정해진 순서로 반환된다 (가위 → 바위 → 보자기, 그 안에서는 코인 값 순).
        /// Id는 호출마다 결정적이다: "Card_{모양문자}{코인값}_{a|b|c}" 형식.
        /// </summary>
        /// <returns>정확히 12장, 총 13코인인 새 리스트.</returns>
        public static List<Card> CreateStandardSet()
        {
            // 00_GameDesign.md §2 구성표:
            //   가위(Scissors): 코인0 1장, 코인1 3장, 코인2 2장  (6장, 7코인)
            //   바위(Rock):     코인0 1장, 코인1 1장, 코인2 1장  (3장, 3코인)
            //   보자기(Paper):  코인0 1장, 코인1 1장, 코인2 1장  (3장, 3코인)
            //   합계:           12장, 13코인
            return new List<Card>
            {
                // 가위 (S) — 6장
                new Card("Card_S0_a", CardShape.Scissors, 0),
                new Card("Card_S1_a", CardShape.Scissors, 1),
                new Card("Card_S1_b", CardShape.Scissors, 1),
                new Card("Card_S1_c", CardShape.Scissors, 1),
                new Card("Card_S2_a", CardShape.Scissors, 2),
                new Card("Card_S2_b", CardShape.Scissors, 2),

                // 바위 (R) — 3장
                new Card("Card_R0_a", CardShape.Rock, 0),
                new Card("Card_R1_a", CardShape.Rock, 1),
                new Card("Card_R2_a", CardShape.Rock, 2),

                // 보자기 (P) — 3장
                new Card("Card_P0_a", CardShape.Paper, 0),
                new Card("Card_P1_a", CardShape.Paper, 1),
                new Card("Card_P2_a", CardShape.Paper, 2),
            };
        }
    }
}