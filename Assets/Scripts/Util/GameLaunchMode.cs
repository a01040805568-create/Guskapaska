namespace Guskapaska.Util
{
    /// <summary>
    /// Game 씬의 진입 모드를 씬 전환 간 전달하는 정적 플래그.
    /// 메인 메뉴에서 설정하고 Game 씬의 GameManager / TutorialController가 읽는다.
    /// 영구 저장이 아닌 런타임 한정 상태라 PlayerPrefs 대신 정적 변수를 사용한다.
    /// </summary>
    public static class GameLaunchMode
    {
        /// <summary>true면 Game 씬을 튜토리얼 모드로 시작한다.</summary>
        public static bool StartInTutorial { get; set; }
    }
}