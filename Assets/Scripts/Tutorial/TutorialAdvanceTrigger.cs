namespace Guskapaska.Tutorial
{
    /// <summary>
    /// 한 튜토리얼 단계가 다음 단계로 넘어가는 조건의 종류.
    /// </summary>
    public enum TutorialAdvanceTrigger
    {
        /// <summary>안내 패널의 "다음" 버튼을 눌러야 진행.</summary>
        NextButton,

        /// <summary>플레이어가 카드를 제출하면 진행.</summary>
        PlayerCardSubmitted,

        /// <summary>라운드 판정이 끝나면 진행.</summary>
        RoundResolved,

        /// <summary>매치가 종료되면 진행.</summary>
        MatchEnded
    }
}