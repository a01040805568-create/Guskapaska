using System;

namespace Guskapaska.Core
{
    /// <summary>
    /// 15초 타이머가 만료됐을 때 플레이어 핸드에서 무작위 카드를 선택하는 정적 유틸.
    /// 자세한 타이머 동작은 00_GameDesign.md §5.1 참고.
    /// </summary>
    public static class AutoSubmitPicker
    {
        /// <summary>
        /// 플레이어 핸드에서 무작위로 카드 한 장을 골라 반환한다.
        /// 핸드는 변경하지 않는다 — 실제 제출 처리는 호출자(GameManager)의 책임이다.
        /// </summary>
        /// <param name="playerHand">플레이어의 현재 핸드. 비어 있으면 안 됨.</param>
        /// <param name="rng">
        /// 사용할 RNG. null이면 <see cref="RandomProvider.Default"/>를 사용해
        /// 게임 전반에서 일관된 난수 소스를 공유한다.
        /// </param>
        /// <returns>선택된 카드.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="playerHand"/>가 null일 때 발생.</exception>
        /// <exception cref="InvalidOperationException">핸드가 비어 있을 때 발생.</exception>
        public static Card PickRandom(Hand playerHand, System.Random rng = null)
        {
            if (playerHand == null)
            {
                throw new ArgumentNullException(nameof(playerHand));
            }
            if (playerHand.IsEmpty)
            {
                throw new InvalidOperationException("빈 핸드에서 자동 제출할 카드를 선택할 수 없습니다.");
            }

            // 명시 RNG가 없으면 게임 공통 RandomProvider를 사용해
            // 시드 고정 시 테스트/재현이 가능하도록 한다.
            System.Random source = rng ?? RandomProvider.Default;
            int index = source.Next(0, playerHand.Count);
            return playerHand.GetAt(index);
        }
    }
}