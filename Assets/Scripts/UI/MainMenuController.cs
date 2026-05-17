using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Util;

namespace Guskapaska.UI
{
    /// <summary>
    /// Wires the four main-menu buttons and the settings panel toggle.
    /// Final tutorial wiring is deferred to Stage 7.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;

        private void Start()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (tutorialButton != null) tutorialButton.onClick.AddListener(OnTutorialClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnStartClicked()
        {
            SceneLoader.LoadGame();
        }

        private void OnTutorialClicked()
        {
            // Final wiring deferred to Stage 7 (see 01_ProjectOverview.md stage breakdown).
            Debug.Log("Tutorial not implemented yet (Stage 7)");
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        private void OnQuitClicked()
        {
            SceneLoader.QuitGame();
        }
    }
}