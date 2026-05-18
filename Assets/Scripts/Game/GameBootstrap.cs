using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Util;

namespace Guskapaska.Game
{
    /// <summary>
    /// Stage 0 placeholder. Wires the "back to menu" button only.
    /// Will be replaced by a real game manager bootstrap in Stage 2
    /// (see 01_ProjectOverview.md stage breakdown).
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private Button backToMenuButton;

        private void Start()
        {
            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(SceneLoader.LoadMainMenu);
        }
    }
}