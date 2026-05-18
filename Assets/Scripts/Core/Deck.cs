using System;
using System.Collections.Generic;

namespace Guskapaska.Core
{
    /// <summary>
    /// 카드의 순서 있는 더미.
    /// 인덱스 0이 "맨 위(top)"로 간주되며, <see cref="DrawTop()"/>은 0번 카드를 꺼낸다.
    /// 셔플은 Fisher-Yates 알고리즘을 사용하며,
    /// 동일한 시드의 <see cref="System.Random"/>을 주입하면 결정적인 결과가 보장된다.
    /// </summary>
    public class Deck
    {
        private readonly List<Card> _cards;

        /// <summary>덱에 남아 있는 카드 수.</summary>
        public int Count => _cards.Count;

        /// <summary>덱이 비어 있는지 여부.</summary>
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>주어진 카드 시퀀스로 덱을 만든다.</summary>
        /// <param name="cards">초기 카드들. null 불가.</param>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/>가 null일 때 발생.</exception>
        public Deck(IEnumerable<Card> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }
            _cards = new List<Card>(cards);
        }

        /// <summary>
        /// Fisher-Yates 알고리즘으로 덱을 제자리에서 셔플한다.
        /// 동일한 시드의 RNG를 넘기면 항상 동일한 순서를 보장한다.
        /// </summary>
        /// <param name="rng">사용할 난수 인스턴스. null이면 시간 기반 시드로 새로 만든다.</param>
        public void Shuffle(System.Random rng = null)
        {
            // RNG가 주어지지 않으면 새 인스턴스 생성 (시간 기반 시드)
            if (rng == null)
            {
                rng = new System.Random();
            }

            // 표준 Fisher-Yates: 뒤에서부터 앞쪽 인덱스를 임의로 골라 교환
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1); // 0 이상 i 이하의 정수
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        /// <summary>
        /// 덱 맨 위(인덱스 0)의 카드 한 장을 뽑아 반환하고 덱에서 제거한다.
        /// </summary>
        /// <returns>맨 위 카드.</returns>
        /// <exception cref="InvalidOperationException">덱이 비어 있을 때 발생.</exception>
        public Card DrawTop()
        {
            if (_cards.Count == 0)
            {
                throw new InvalidOperationException("빈 덱에서 카드를 뽑을 수 없습니다.");
            }
            Card top = _cards[0];
            _cards.RemoveAt(0);
            return top;
        }

        /// <summary>
        /// 덱 맨 위에서부터 <paramref name="count"/>장의 카드를 한 번에 뽑는다.
        /// 반환되는 리스트의 0번 원소가 원래 덱의 맨 위 카드이다.
        /// </summary>
        /// <param name="count">뽑을 카드 수.</param>
        /// <returns>뽑힌 카드들의 새 리스트.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/>가 음수일 때 발생.</exception>
        /// <exception cref="InvalidOperationException">남은 카드 수보다 많이 뽑으려 할 때 발생.</exception>
        public List<Card> DrawTop(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count), count, "뽑을 카드 수는 0 이상이어야 합니다.");
            }
            if (count > _cards.Count)
            {
                throw new InvalidOperationException(
                    $"덱에 남은 카드({_cards.Count})보다 많은 수({count})를 뽑을 수 없습니다.");
            }

            List<Card> drawn = new List<Card>(count);
            for (int i = 0; i < count; i++)
            {
                drawn.Add(_cards[0]);
                _cards.RemoveAt(0);
            }
            return drawn;
        }

        /// <summary>덱에 남은 카드의 읽기 전용 뷰를 반환한다 (현재 순서 그대로).</summary>
        public IReadOnlyList<Card> Peek()
        {
            return _cards.AsReadOnly();
        }
    }
}