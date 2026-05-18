using System;
using System.Collections.Generic;
 
namespace Guskapaska.Core
{
    /// <summary>
    /// 플레이어 또는 AI가 들고 있는 카드 묶음.
    /// 내부적으로 <see cref="List{Card}"/>로 백업되며,
    /// <see cref="Cards"/>는 외부에서 변경할 수 없는 읽기 전용 뷰를 노출한다.
    /// 카드의 동등성은 <see cref="Card.Id"/>에 기반한다 (Card 클래스 참고).
    /// </summary>
    public class Hand
    {
        private readonly List<Card> _cards;
 
        /// <summary>읽기 전용 카드 리스트.</summary>
        public IReadOnlyList<Card> Cards => _cards;
 
        /// <summary>현재 들고 있는 카드 수.</summary>
        public int Count => _cards.Count;
 
        /// <summary>카드가 한 장도 없으면 true.</summary>
        public bool IsEmpty => _cards.Count == 0;
 
        /// <summary>빈 핸드를 생성한다.</summary>
        public Hand()
        {
            _cards = new List<Card>();
        }
 
        /// <summary>초기 카드들로 핸드를 생성한다.</summary>
        /// <param name="initial">초기 카드 컬렉션. null이면 빈 핸드가 된다.</param>
        public Hand(IEnumerable<Card> initial)
        {
            _cards = initial == null ? new List<Card>() : new List<Card>(initial);
        }
 
        /// <summary>카드 한 장을 핸드에 추가한다.</summary>
        /// <param name="card">추가할 카드. null 불가.</param>
        /// <exception cref="ArgumentNullException"><paramref name="card"/>가 null일 때 발생.</exception>
        public void Add(Card card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }
            _cards.Add(card);
        }
 
        /// <summary>여러 장의 카드를 한 번에 추가한다.</summary>
        /// <param name="cards">추가할 카드 컬렉션. null 불가.</param>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/>가 null일 때 발생.</exception>
        public void AddRange(IEnumerable<Card> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }
            foreach (Card c in cards)
            {
                Add(c);
            }
        }
 
        /// <summary>
        /// 핸드에서 카드를 제거한다 (Id 기반 비교).
        /// </summary>
        /// <param name="card">제거할 카드.</param>
        /// <returns>제거에 성공하면 true, 핸드에 없었으면 false.</returns>
        public bool Remove(Card card)
        {
            if (card == null)
            {
                return false;
            }
            return _cards.Remove(card);
        }
 
        /// <summary>핸드에 해당 카드가 있는지 (Id 기반) 확인한다.</summary>
        public bool Contains(Card card)
        {
            if (card == null)
            {
                return false;
            }
            return _cards.Contains(card);
        }
 
        /// <summary>인덱스로 카드를 조회한다.</summary>
        /// <param name="index">0부터 시작하는 인덱스.</param>
        /// <exception cref="ArgumentOutOfRangeException">인덱스가 범위를 벗어나면 발생.</exception>
        public Card GetAt(int index)
        {
            if (index < 0 || index >= _cards.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    $"인덱스가 핸드 범위(0~{_cards.Count - 1})를 벗어났습니다.");
            }
            return _cards[index];
        }
 
        /// <summary>핸드의 모든 카드를 제거한다.</summary>
        public void Clear()
        {
            _cards.Clear();
        }
    }
}