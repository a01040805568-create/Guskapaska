using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Guskapaska.Util
{
    /// <summary>
    /// Static utility for scene navigation and application lifecycle.
    /// See 01_ProjectOverview.md for build index conventions.
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>Build index 0.</summary>
        public const string MainMenuScene = "MainMenu";

        /// <summary>Build index 1.</summary>
        public const string GameScene = "Game";

        /// <summary>
        /// Loads the main menu scene by name.
        /// </summary>
        public static void LoadMainMenu()
        {
            SceneManager.LoadScene(MainMenuScene);
        }

        /// <summary>
        /// Loads the game scene by name.
        /// </summary>
        public static void LoadGame()
        {
            SceneManager.LoadScene(GameScene);
        }

        /// <summary>
        /// Quits the application. In the editor, stops play mode instead.
        /// </summary>
        public static void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}