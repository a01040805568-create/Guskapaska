# Stage 0 — Main Menu & Scene Infrastructure

## Context
- Refer to `00_GameDesign.md` for all game rules.
- Refer to `01_ProjectOverview.md` for tech stack, namespaces, folder structure, and conventions.
- Do not duplicate game rules or conventions in code comments; reference the docs instead.

## Stage Goal
Build the application shell: main menu scene, settings panel, scene transitions, audio/display persistence managers, and a placeholder Game scene.
**Do NOT implement** cards, gameplay logic, animations, or tutorial content in this stage.

---

## Deliverables

### 1. Scripts

All scripts live under `Assets/Scripts/` in folders matching their sub-namespace (`Audio/`, `Util/`, `UI/`, `Game/`).

#### `SceneLoader.cs` — namespace `Guskapaska.Util`
Static utility for scene navigation.

```csharp
public static class SceneLoader
{
    public const string MainMenuScene = "MainMenu";
    public const string GameScene = "Game";

    public static void LoadMainMenu();
    public static void LoadGame();
    public static void QuitGame();
}
```

- `QuitGame()` must use `#if UNITY_EDITOR` to call `EditorApplication.isPlayingChanged = false`; otherwise `Application.Quit()`.

#### `AudioManager.cs` — namespace `Guskapaska.Audio`
MonoBehaviour singleton with `DontDestroyOnLoad`.

```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    public float MasterVolume { get; private set; }   // linear 0.0 - 1.0
    public float BgmVolume    { get; private set; }
    public float SfxVolume    { get; private set; }

    private void Awake();
    public void SetMasterVolume(float linear01);
    public void SetBgmVolume(float linear01);
    public void SetSfxVolume(float linear01);
    public void PlaySfx(AudioClip clip);
    public void PlayBgm(AudioClip clip, bool loop = true);
    public void StopBgm();
    private void LoadFromPrefs();
    private void SaveToPrefs();
}
```

- Singleton dedup: if `Instance` already exists, `Destroy(gameObject)` and return early.
- Volume conversion: `dB = (v <= 0.0001f) ? -80f : Mathf.Log10(v) * 20f`.
- Mixer exposed parameter names: `"MasterVolume"`, `"BgmVolume"`, `"SfxVolume"`.
- Save to prefs on every `Set*Volume` call.
- Set `Application.targetFrameRate = 60` in `Awake`.

#### `DisplayManager.cs` — namespace `Guskapaska.Util`
MonoBehaviour singleton with `DontDestroyOnLoad`.

```csharp
public class DisplayManager : MonoBehaviour
{
    public static DisplayManager Instance { get; private set; }

    public bool IsFullscreen { get; private set; }

    private void Awake();
    public void SetFullscreen(bool fullscreen);
    private void ApplyFromPrefs();
}
```

- Always applies resolution 1920×1080 regardless of mode (fullscreen vs windowed).
- Use `Screen.SetResolution(1920, 1080, fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed)`.
- Save pref on every `SetFullscreen` call.

#### `GameSettings.cs` — namespace `Guskapaska.Util`
Static facade for non-audio/non-display prefs.

```csharp
public static class GameSettings
{
    private const string KEY_HAS_PLAYED = "has_played_before";

    public static bool HasPlayedBefore { get; }
    public static void MarkAsPlayed();
    public static void ResetAll();   // editor/debug helper: PlayerPrefs.DeleteAll
}
```

#### `MainMenuController.cs` — namespace `Guskapaska.UI`
MonoBehaviour, attached in MainMenu scene.

```csharp
public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;

    private void Start();
    private void OnStartClicked();
    private void OnTutorialClicked();
    private void OnSettingsClicked();
    private void OnQuitClicked();
}
```

- `OnTutorialClicked` for Stage 0: `Debug.Log("Tutorial not implemented yet (Stage 7)")`. Final wiring in Stage 7.
- `settingsPanel` is inactive by default; `OnSettingsClicked` calls `settingsPanel.SetActive(true)`.

#### `SettingsPanelController.cs` — namespace `Guskapaska.UI`
MonoBehaviour, attached to SettingsPanel.

```csharp
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

    private void OnEnable();
    private void OnMasterChanged(float v);
    private void OnBgmChanged(float v);
    private void OnSfxChanged(float v);
    private void OnFullscreenChanged(bool isOn);
    private void OnCloseClicked();
}
```

- `OnEnable` reads current values from `AudioManager.Instance` and `DisplayManager.Instance` and applies them to sliders/toggle without firing the change callbacks (use `SetValueWithoutNotify`).
- After `OnEnable` initialization, wire up listeners.
- All sliders range 0.0 to 1.0, whole-number = false.

#### `GameBootstrap.cs` — namespace `Guskapaska.Game`
Placeholder for Stage 0 only.

```csharp
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private Button backToMenuButton;

    private void Start();   // wires backToMenuButton -> SceneLoader.LoadMainMenu()
}
```

This script will be **replaced** in Stage 2 with a real game manager bootstrap.

---

### 2. Scenes

#### `MainMenu.unity`

```
Canvas (Screen Space - Overlay)
  - CanvasScaler: Scale With Screen Size, Reference 1920x1080, Match 0.5
  Background (Image, anchor stretch full, dark placeholder color)
  TitleText (TextMeshProUGUI, anchor top-center, text "구스카파스카", large font)
  ButtonContainer (Vertical Layout Group, center anchored, spacing 20)
    StartButton    (text "게임 시작")
    TutorialButton (text "튜토리얼")
    SettingsButton (text "설정")
    QuitButton     (text "종료")
  SettingsPanel (GameObject, inactive by default)
    PanelBg (Image, full-stretched, semi-transparent black, RaycastTarget on)
    PanelWindow (Image, centered, size 800x600)
      HeaderText (TMP, text "설정")
      MasterVolumeRow
        Label (TMP, "전체 볼륨")
        MasterVolumeSlider (range 0–1)
      BgmVolumeRow
        Label (TMP, "배경음 볼륨")
        BgmVolumeSlider (range 0–1)
      SfxVolumeRow
        Label (TMP, "효과음 볼륨")
        SfxVolumeSlider (range 0–1)
      FullscreenRow
        Label (TMP, "전체화면")
        FullscreenToggle
      CloseButton (text "닫기")
EventSystem
AudioManager (GameObject with AudioManager + AudioSource(BGM) + AudioSource(SFX) + Mixer ref)
DisplayManager (GameObject with DisplayManager)
MainMenuControllerHost (GameObject with MainMenuController)
```

#### `Game.unity` (Stage 0 placeholder)

```
Canvas (same scaler settings)
  BackToMenuButton (top-left, text "메인 메뉴")
  PlaceholderText (centered, text "게임 씬 — 이후 단계에서 구현")
EventSystem
GameBootstrap (GameObject with GameBootstrap)
```

---

### 3. AudioMixer

Create `Assets/Audio/MainMixer.mixer`:

1. Right-click in `Assets/Audio/` → Create → Audio Mixer → name `MainMixer`.
2. In Mixer window, add child groups under `Master`: `BGM`, `SFX`.
3. Right-click each group's Volume parameter in the Inspector → **Expose 'Volume of …' to script**.
4. In the top-right of the mixer window, rename exposed parameters to:
   - Master → `MasterVolume`
   - BGM → `BgmVolume`
   - SFX → `SfxVolume`
5. Assign the mixer to AudioManager's `mixer` field.
6. Assign BGM AudioSource → output to BGM group; SFX AudioSource → output to SFX group.

---

### 4. Build Settings

- Add scenes in order: MainMenu (0), Game (1).
- Default Quality Settings: VSync off, but `Application.targetFrameRate = 60` in code.
- Player Settings:
  - Default Resolution: 1920×1080
  - Resizable Window: off
  - Allowed Orientations: Landscape only (not relevant for desktop but set for safety)

---

## Korean Font

- All TextMeshProUGUI components need a Korean-capable TMP font asset.
- Recommend `NotoSansKR-Regular` or `Pretendard`. Generate a TMP Font Asset from the source TTF with a character set covering common Korean glyphs.
- Add comment in any script that creates dynamic TMP text: `// TODO: ensure TMP font asset supports Korean glyphs`.

---

## Output Format (what the AI should produce)

1. **File tree**: list every new file with full path under `Assets/`.
2. **Source code**: complete contents of every `.cs` file.
3. **Editor setup checklist**: numbered steps to assemble both scenes, audio mixer, and build settings from scratch in Unity.
4. **Verification steps**: a short test plan, e.g.:
   - Launch from MainMenu → click 게임 시작 → Game scene loads → click 메인 메뉴 → returns.
   - Open Settings → adjust master volume → close → reopen → value persisted.
   - Toggle fullscreen → restart Unity → toggle state persisted.
   - Click Tutorial → console shows Stage 7 placeholder log.

---

## Constraints

- No third-party packages.
- No game logic, cards, gameplay scripts, or animations.
- All public methods documented with English XML comments.
- All in-game text strings in Korean.
- Singletons must self-deduplicate across scene loads.
