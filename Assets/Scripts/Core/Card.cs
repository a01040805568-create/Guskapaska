using System;

namespace Guskapaska.Core
{
    /// <summary>
    /// 불변 카드 객체.
    /// 각 카드는 고유한 <see cref="Id"/>, 가위바위보 판정용 <see cref="Shape"/>,
    /// 보석 보상에 사용되는 <see cref="CoinValue"/> (0, 1, 2 중 하나)를 가진다.
    /// 동등성은 오직 <see cref="Id"/>에만 기반한다.
    /// 카드 구성은 00_GameDesign.md §2 참고.
    /// </summary>
    public class Card
    {
        /// <summary>카드 고유 식별자 (예: "Card_S1_a", "Card_R0_a", "Card_P2_a").</summary>
        public string Id { get; }

        /// <summary>카드 모양 (가위 / 바위 / 보자기).</summary>
        public CardShape Shape { get; }

        /// <summary>카드에 인쇄된 코인 값. 반드시 0, 1, 2 중 하나.</summary>
        public int CoinValue { get; }

        /// <summary>
        /// 카드 생성자.
        /// </summary>
        /// <param name="id">고유 식별자. null이거나 빈 문자열 불가.</param>
        /// <param name="shape">카드 모양.</param>
        /// <param name="coinValue">코인 값 (0, 1, 2 중 하나).</param>
        /// <exception cref="ArgumentNullException"><paramref name="id"/>가 null일 때 발생.</exception>
        /// <exception cref="ArgumentException"><paramref name="id"/>가 빈 문자열일 때 발생.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="coinValue"/>가 0, 1, 2가 아닐 때 발생.</exception>
        public Card(string id, CardShape shape, int coinValue)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }
            if (id.Length == 0)
            {
                throw new ArgumentException("카드 Id는 빈 문자열일 수 없습니다.", nameof(id));
            }
            if (coinValue < 0 || coinValue > 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coinValue),
                    coinValue,
                    "CoinValue는 0, 1, 2 중 하나여야 합니다.");
            }

            Id = id;
            Shape = shape;
            CoinValue = coinValue;
        }

        /// <summary>디버그용 문자열 표현 (예: "Card_S1_a(Scissors,1)").</summary>
        public override string ToString()
        {
            return $"{Id}({Shape},{CoinValue})";
        }

        /// <summary>동등성 비교는 <see cref="Id"/>에만 기반한다.</summary>
        public override bool Equals(object obj)
        {
            if (obj is Card other)
            {
                return Id == other.Id;
            }
            return false;
        }

        /// <summary>해시 코드는 <see cref="Id"/>로부터 생성된다.</summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}