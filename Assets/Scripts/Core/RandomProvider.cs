using System;

namespace Guskapaska.Core
{
    /// <summary>
    /// 게임 로직 전반에 걸친 결정적 난수 소스의 중앙 진입점.
    /// AI의 카드 선택, 덱 셔플, 타이머 만료 시 자동 제출 등에 사용된다.
    /// 테스트에서는 <see cref="Seed"/>로 시드를 고정해 재현 가능한 결과를 얻을 수 있다.
    /// 02_Unity6_Guidelines.md 정책에 따라 UnityEngine.Random 대신 System.Random을 사용한다.
    /// </summary>
    public static class RandomProvider
    {
        /// <summary>
        /// 기본 난수 인스턴스. 테스트나 재현을 위해 다시 시드할 수 있다.
        /// </summary>
        public static System.Random Default { get; private set; } = new System.Random();

        /// <summary>
        /// 지정한 시드로 <see cref="Default"/>를 새 인스턴스로 교체한다.
        /// </summary>
        /// <param name="seed">난수 시드 값.</param>
        public static void Seed(int seed)
        {
            Default = new System.Random(seed);
        }

        /// <summary>
        /// 시간 기반 시드의 새 인스턴스로 <see cref="Default"/>를 교체한다.
        /// </summary>
        public static void Reset()
        {
            Default = new System.Random();
        }
    }
}