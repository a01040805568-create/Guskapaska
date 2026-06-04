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

            // 씬 진입 fade-in은 SceneTransition이 자체 Start에서 처리하므로 여기서 호출하지 않는다.
        }

        private void OnStartClicked()
        {
            // 페이드 아웃 후 게임 씬 로드 (트랜지션이 없으면 즉시 로드로 폴백).
            SceneLoader.LoadSceneWithFade(SceneLoader.GameScene, sceneTransition);
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
