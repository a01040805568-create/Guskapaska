using UnityEngine;

namespace Guskapaska.Util
{
    /// <summary>
    /// Static facade for non-audio, non-display PlayerPrefs entries.
    /// Keys are defined in 01_ProjectOverview.md.
    /// </summary>
    public static class GameSettings
    {
        private const string KEY_HAS_PLAYED = "has_played_before";

        /// <summary>
        /// True once the player has completed at least one game.
        /// Used to suggest the tutorial on first launch.
        /// </summary>
        public static bool HasPlayedBefore
        {
            get { return PlayerPrefs.GetInt(KEY_HAS_PLAYED, 0) != 0; }
        }

        /// <summary>
        /// Marks the player as having completed at least one game.
        /// </summary>
        public static void MarkAsPlayed()
        {
            PlayerPrefs.SetInt(KEY_HAS_PLAYED, 1);
            PlayerPrefs.Save();
        }

                private const string KEY_HAS_SEEN_TUTORIAL = "has_seen_tutorial";

        /// <summary>
        /// 플레이어가 튜토리얼을 완료했거나 건너뛴 적이 있으면 true.
        /// </summary>
        public static bool HasSeenTutorial
        {
            get { return PlayerPrefs.GetInt(KEY_HAS_SEEN_TUTORIAL, 0) != 0; }
        }

        /// <summary>
        /// 첫 실행 시 튜토리얼을 권장해야 하는지 여부.
        /// 실제 매치 경험과 튜토리얼 시청 중 하나라도 있으면 권장하지 않는다.
        /// </summary>
        public static bool ShouldRecommendTutorial
        {
            get { return !HasPlayedBefore && !HasSeenTutorial; }
        }

        /// <summary>
        /// 플레이어가 튜토리얼을 완료하거나 건너뛰었음을 기록한다.
        /// </summary>
        public static void MarkTutorialSeen()
        {
            PlayerPrefs.SetInt(KEY_HAS_SEEN_TUTORIAL, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Editor/debug helper: clears every PlayerPrefs entry.
        /// </summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}