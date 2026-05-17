using UnityEngine;

namespace Guskapaska.Util
{
    /// <summary>
    /// Persistent singleton that manages window mode (fullscreen vs windowed)
    /// at a fixed 1920x1080 resolution. State is persisted via PlayerPrefs.
    /// PlayerPrefs keys are defined in 01_ProjectOverview.md.
    /// </summary>
    public class DisplayManager : MonoBehaviour
    {
        /// <summary>Globally accessible instance.</summary>
        public static DisplayManager Instance { get; private set; }

        private const string KEY_FULLSCREEN = "display_fullscreen";
        private const int DEFAULT_FULLSCREEN = 1;

        private const int TARGET_WIDTH = 1920;
        private const int TARGET_HEIGHT = 1080;

        /// <summary>True if the application is currently in fullscreen.</summary>
        public bool IsFullscreen { get; private set; }

        private void Awake()
        {
            // Self-deduplicating singleton across scene loads.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            ApplyFromPrefs();
        }

        /// <summary>
        /// Switches between fullscreen and windowed mode. Always applies
        /// the 1920x1080 resolution. Persists the new state to PlayerPrefs.
        /// </summary>
        public void SetFullscreen(bool fullscreen)
        {
            IsFullscreen = fullscreen;

            FullScreenMode mode = fullscreen
                ? FullScreenMode.ExclusiveFullScreen
                : FullScreenMode.Windowed;

            Screen.SetResolution(TARGET_WIDTH, TARGET_HEIGHT, mode);

            PlayerPrefs.SetInt(KEY_FULLSCREEN, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplyFromPrefs()
        {
            int stored = PlayerPrefs.GetInt(KEY_FULLSCREEN, DEFAULT_FULLSCREEN);
            bool fullscreen = stored != 0;

            IsFullscreen = fullscreen;

            FullScreenMode mode = fullscreen
                ? FullScreenMode.ExclusiveFullScreen
                : FullScreenMode.Windowed;

            Screen.SetResolution(TARGET_WIDTH, TARGET_HEIGHT, mode);
        }
    }
}