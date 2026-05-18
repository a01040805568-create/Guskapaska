using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Audio;
using Guskapaska.Util;

namespace Guskapaska.UI
{
    /// <summary>
    /// Binds the settings panel UI to AudioManager and DisplayManager.
    /// Reads current values on enable and pushes changes back via callbacks.
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        [Header("Volume")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Display")]
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Navigation")]
        [SerializeField] private Button closeButton;

        private bool _listenersWired;

        private void OnEnable()
        {
            // Detach listeners while we sync initial values to avoid
            // firing change callbacks during initialization.
            DetachListeners();

            if (AudioManager.Instance != null)
            {
                if (masterVolumeSlider != null)
                    masterVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);

                if (bgmVolumeSlider != null)
                    bgmVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.BgmVolume);

                if (sfxVolumeSlider != null)
                    sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
            }

            if (DisplayManager.Instance != null && fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(DisplayManager.Instance.IsFullscreen);
            }

            AttachListeners();
        }

        private void OnDisable()
        {
            DetachListeners();
        }

        private void AttachListeners()
        {
            if (_listenersWired) return;

            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterChanged);
            if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.AddListener(OnBgmChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSfxChanged);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);

            _listenersWired = true;
        }

        private void DetachListeners()
        {
            if (!_listenersWired) return;

            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(OnMasterChanged);
            if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxChanged);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);

            _listenersWired = false;
        }

        private void OnMasterChanged(float v)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMasterVolume(v);
        }

        private void OnBgmChanged(float v)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetBgmVolume(v);
        }

        private void OnSfxChanged(float v)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSfxVolume(v);
        }

        private void OnFullscreenChanged(bool isOn)
        {
            if (DisplayManager.Instance != null)
                DisplayManager.Instance.SetFullscreen(isOn);
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }
    }
}