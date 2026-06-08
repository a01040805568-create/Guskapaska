using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Util;

namespace Guskapaska.UI
{
    /// <summary>
    /// Wires the four main-menu buttons and the settings panel toggle.
    /// Stage 6 routes the Start button through a fade transition.
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

        [Header("Transition (Stage 6)")]
        [Tooltip("게임 씬으로 넘어갈 때 페이드 아웃에 사용. 비워두면 즉시 전환된다.")]
        [SerializeField] private SceneTransition sceneTransition;

        private void Start()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (tutorialButton != null) tutorialButton.onClick.AddListener(OnTutorialClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

            // 첫 실행(플레이·튜토리얼 모두 경험 없음)이면 자동으로 튜토리얼을 시작한다.
            if (GameSettings.ShouldRecommendTutorial)
            {
                OnTutorialClicked();
            }
        }

        private void OnStartClicked()
        {
            // 일반 플레이 모드로 진입.
            GameLaunchMode.StartInTutorial = false;
            SceneLoader.LoadGame();
        }

        private void OnTutorialClicked()
        {
            // 튜토리얼 모드로 진입.
            GameLaunchMode.StartInTutorial = true;
            SceneLoader.LoadGame();
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
