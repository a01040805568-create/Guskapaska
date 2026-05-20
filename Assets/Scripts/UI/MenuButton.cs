using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Util;

namespace Guskapaska.UI
{
    /// <summary>
    /// Wires a button to return the player to the MainMenu scene.
    /// Replaces the Stage 0 GameBootstrap placeholder behavior.
    /// </summary>
    public class MenuButton : MonoBehaviour
    {
        [SerializeField] private Button backToMenuButton;

        private void Start()
        {
            if (backToMenuButton == null)
            {
                Debug.LogError("[MenuButton] Button reference is missing.");
                return;
            }

            // 클릭 시 메인 메뉴 씬으로 전환 (Stage 0의 SceneLoader 사용)
            backToMenuButton.onClick.AddListener(SceneLoader.LoadMainMenu);
        }
    }
}