using System;
using System.Collections.Generic;

namespace Guskapaska.Core
{
    /// <summary>
    /// 튜토리얼용 AI 전략. 라운드 순서대로 미리 지정한 모양의 카드를 손패에서 골라 낸다.
    /// 지정 시퀀스를 모두 소진하면 fallback 전략(보통 무작위)에 위임한다.
    /// 시연 시나리오(무승부·승리 등)를 결정적으로 재현하기 위해 사용한다.
    /// </summary>
    public class ScriptedAiStrategy : IAiStrategy
    {
        // 라운드별로 AI가 낼 카드 모양. 인덱스 0이 첫 라운드.
        private readonly IReadOnlyList<CardShape> _scriptedShapes;

        // 시퀀스 소진 후(자유 플레이 구간) 사용할 대체 전략.
        private readonly IAiStrategy _fallback;

        // 다음에 사용할 시퀀스 인덱스.
        private int _index;

        /// <summary>
        /// 스크립트 전략을 생성한다.
        /// </summary>
        /// <param name="scriptedShapes">라운드 순서대로 AI가 낼 모양 목록.</param>
        /// <param name="fallback">시퀀스 소진 후 위임할 전략. null 불가.</param>
        /// <exception cref="ArgumentNullException">인자가 null일 때 발생.</exception>
        public ScriptedAiStrategy(IReadOnlyList<CardShape> scriptedShapes, IAiStrategy fallback)
        {
            _scriptedShapes = scriptedShapes ?? throw new ArgumentNullException(nameof(scriptedShapes));
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
            _index = 0;
        }

        /// <summary>
        /// 현재 라운드에 지정된 모양의 카드를 손패에서 찾아 반환한다.
        /// 지정 모양이 손패에 없거나 시퀀스를 소진했으면 fallback 전략에 위임한다.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="aiHand"/>가 null일 때 발생.</exception>
        /// <exception cref="InvalidOperationException">손패가 비어 있을 때 발생.</exception>
        public Card SelectCard(Hand aiHand)
        {
            if (aiHand == null)
            {
                throw new ArgumentNullException(nameof(aiHand));
            }
            if (aiHand.IsEmpty)
            {
                throw new InvalidOperationException("빈 핸드에서 카드를 선택할 수 없습니다.");
            }

            // 스크립트 시퀀스가 남아 있으면 지정 모양 카드를 우선 탐색한다.
            if (_index < _scriptedShapes.Count)
            {
                CardShape want = _scriptedShapes[_index];
                _index++;

                Card found = FindByShape(aiHand, want);
                if (found != null)
                {
                    return found;
                }
                // 지정 모양 카드가 손패에 없으면(방어적 처리) 아래 fallback으로 진행한다.
            }

            // 시퀀스 소진 또는 지정 카드 없음 → 자유 플레이 전략에 위임.
            return _fallback.SelectCard(aiHand);
        }

        // 손패에서 지정 모양의 첫 카드를 찾는다. 없으면 null.
        private static Card FindByShape(Hand hand, CardShape shape)
        {
            IReadOnlyList<Card> cards = hand.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].Shape == shape)
                {
                    return cards[i];
                }
            }
            return null;
        }
    }
}