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