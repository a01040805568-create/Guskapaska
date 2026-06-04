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

        /// <summary>
        /// Load a scene through the active SceneTransition fader if present;
        /// otherwise load it immediately.
        /// </summary>
        public static void LoadSceneWithFade(string sceneName, SceneTransition transition)
        {
            if (transition != null)
            {
                // fade-out 코루틴은 트랜지션 자신을 호스트로 실행하며, 페이드가 끝나면 씬을 로드한다.
                transition.StartCoroutine(
                    transition.FadeOut(() => SceneManager.LoadScene(sceneName)));
            }
            else
            {
                // 트랜지션이 없으면 즉시 로드로 폴백.
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
