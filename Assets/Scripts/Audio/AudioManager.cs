using UnityEngine;
using UnityEngine.Audio;

namespace Guskapaska.Audio
{
    /// <summary>
    /// Persistent singleton that controls BGM/SFX playback and exposes
    /// linear (0..1) volume properties backed by PlayerPrefs.
    /// PlayerPrefs keys are defined in 01_ProjectOverview.md.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        /// <summary>Globally accessible instance.</summary>
        public static AudioManager Instance { get; private set; }

        private const string PARAM_MASTER = "MasterVolume";
        private const string PARAM_BGM = "BgmVolume";
        private const string PARAM_SFX = "SfxVolume";

        private const string KEY_MASTER = "vol_master";
        private const string KEY_BGM = "vol_bgm";
        private const string KEY_SFX = "vol_sfx";

        private const float DEFAULT_MASTER = 1.0f;
        private const float DEFAULT_BGM = 0.8f;
        private const float DEFAULT_SFX = 1.0f;

        private const float SILENCE_THRESHOLD = 0.0001f;
        private const float SILENCE_DB = -80f;

        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        public float MasterVolume { get; private set; }
        public float BgmVolume { get; private set; }
        public float SfxVolume { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;

            LoadFromPrefs();
        }

        public void SetMasterVolume(float linear01)
        {
            MasterVolume = Mathf.Clamp01(linear01);
            ApplyMixerParam(PARAM_MASTER, MasterVolume);
            SaveToPrefs();
        }

        public void SetBgmVolume(float linear01)
        {
            BgmVolume = Mathf.Clamp01(linear01);
            ApplyMixerParam(PARAM_BGM, BgmVolume);
            SaveToPrefs();
        }

        public void SetSfxVolume(float linear01)
        {
            SfxVolume = Mathf.Clamp01(linear01);
            ApplyMixerParam(PARAM_SFX, SfxVolume);
            SaveToPrefs();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            if (clip == null || bgmSource == null) return;
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }

        public void StopBgm()
        {
            if (bgmSource == null) return;
            bgmSource.Stop();
        }

        private void LoadFromPrefs()
        {
            MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER, DEFAULT_MASTER);
            BgmVolume = PlayerPrefs.GetFloat(KEY_BGM, DEFAULT_BGM);
            SfxVolume = PlayerPrefs.GetFloat(KEY_SFX, DEFAULT_SFX);

            ApplyMixerParam(PARAM_MASTER, MasterVolume);
            ApplyMixerParam(PARAM_BGM, BgmVolume);
            ApplyMixerParam(PARAM_SFX, SfxVolume);
        }

        private void SaveToPrefs()
        {
            PlayerPrefs.SetFloat(KEY_MASTER, MasterVolume);
            PlayerPrefs.SetFloat(KEY_BGM, BgmVolume);
            PlayerPrefs.SetFloat(KEY_SFX, SfxVolume);
            PlayerPrefs.Save();
        }

        private void ApplyMixerParam(string param, float linear01)
        {
            if (mixer == null) return;

            float db = (linear01 <= SILENCE_THRESHOLD)
                ? SILENCE_DB
                : Mathf.Log10(linear01) * 20f;

            mixer.SetFloat(param, db);
        }
    }
}