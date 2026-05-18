namespace Guskapaska.Core
{
    /// <summary>
    /// 가위바위보 비교 결과.
    /// </summary>
    public enum RpsResult
    {
        /// <summary>같은 모양 — 무승부.</summary>
        Draw,
        /// <summary>왼쪽(첫 번째 인자) 승.</summary>
        LeftWins,
        /// <summary>오른쪽(두 번째 인자) 승.</summary>
        RightWins
    }

    /// <summary>
    /// 가위바위보 판정 유틸.
    /// 표준 규칙: 가위는 보자기를, 보자기는 바위를, 바위는 가위를 이긴다.
    /// 같은 모양은 무승부 (00_GameDesign.md §5.2 참고).
    /// </summary>
    public static class RpsJudge
    {
        /// <summary>
        /// 두 모양을 비교해 승패를 반환한다.
        /// </summary>
        /// <param name="left">왼쪽(첫 번째) 모양.</param>
        /// <param name="right">오른쪽(두 번째) 모양.</param>
        /// <returns>무승부 / 왼쪽 승 / 오른쪽 승.</returns>
        public static RpsResult Compare(CardShape left, CardShape right)
        {
            if (left == right)
            {
                return RpsResult.Draw;
            }

            // 왼쪽이 이기는 세 가지 경우만 명시적으로 검사하고,
            // 그 외는 자동으로 오른쪽 승으로 처리한다.
            bool leftWins =
                (left == CardShape.Scissors && right == CardShape.Paper) ||
                (left == CardShape.Paper && right == CardShape.Rock) ||
                (left == CardShape.Rock && right == CardShape.Scissors);

            return leftWins ? RpsResult.LeftWins : RpsResult.RightWins;
        }
    }
}
