using System;

namespace Guskapaska.Core
{
    /// <summary>
    /// AI 카드 선택 전략 — Stage 1에서는 단순 무작위.
    /// 향후 다른 전략(카드 카운팅, 난이도 등)을 도입할 때 공통 인터페이스를 빼낼 수 있으나
    /// Stage 1 범위에서는 일부러 인터페이스를 만들지 않는다 (불필요한 추상화 회피).
    /// </summary>
    public class AiRandomStrategy
    {
        private readonly System.Random _rng;

        /// <summary>
        /// 사용할 난수 인스턴스를 주입해 생성한다.
        /// </summary>
        /// <param name="rng">사용할 RNG. null이면 시간 기반 시드로 새 인스턴스를 만든다.</param>
        public AiRandomStrategy(System.Random rng = null)
        {
            _rng = rng ?? new System.Random();
        }

        /// <summary>
        /// AI 핸드에서 무작위로 카드 한 장을 선택해 반환한다.
        /// 핸드는 변경하지 않는다 — 실제 핸드에서 제거하는 책임은 호출자에게 있다.
        /// </summary>
        /// <param name="aiHand">AI의 현재 핸드. 비어 있으면 안 됨.</param>
        /// <returns>선택된 카드.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="aiHand"/>가 null일 때 발생.</exception>
        /// <exception cref="InvalidOperationException">핸드가 비어 있을 때 발생.</exception>
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

            int index = _rng.Next(0, aiHand.Count);
            return aiHand.GetAt(index);
        }
    }
}