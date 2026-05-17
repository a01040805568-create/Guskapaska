# 02. Unity 6 Guidelines & Pitfalls

This document captures Unity 6 (6000.x) specific rules and API changes that must be followed.
Older Unity versions (2022, 2023, 2024) had different APIs and Editor UIs — **ignore any code or instructions that match those older patterns**.

This document grows as new pitfalls are discovered during development.
When a wrong-version answer is encountered, add the case here.

---

## ⚠️ How To Use This Document

When generating or reviewing code for this project:
1. **Always assume Unity 6.3 LTS (6000.3.15f1)** — never target older versions.
2. **Verify against every rule below** before producing C# code, Editor instructions, or Player Settings guidance.
3. If a needed API is not listed here, prefer the most recently-documented Unity 6 API. Do not fall back to 2022/2023 patterns.
4. If unsure, state the uncertainty in the response rather than guessing.

---

## 1. Find APIs — Use `…ByType`, NOT `…OfType`

`FindObjectOfType` and `FindObjectsOfType` are **deprecated** in Unity 6 and emit obsolete warnings. Use the new trio:

| ❌ Deprecated (do not use) | ✅ Unity 6 replacement |
|---|---|
| `Object.FindObjectOfType<T>()` | `Object.FindFirstObjectByType<T>()` (same semantics) **or** `Object.FindAnyObjectByType<T>()` (faster, returns any match) |
| `Object.FindObjectsOfType<T>()` | `Object.FindObjectsByType<T>(FindObjectsSortMode.None)` (faster, unsorted) **or** `FindObjectsByType<T>(FindObjectsSortMode.InstanceID)` (matches old sorted behavior) |
| `Object.FindObjectsOfType<T>(true)` | `Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)` |

**Default choice for this project:**
- Singleton lookup → `FindFirstObjectByType<T>()`
- Bulk lookup → `FindObjectsByType<T>(FindObjectsSortMode.None)` (we don't need sorted)
- Include inactive only when explicitly needed.

```csharp
// ❌ DO NOT
var mgr = FindObjectOfType<GameManager>();
var all = FindObjectsOfType<Card>();

// ✅ DO
var mgr = FindFirstObjectByType<GameManager>();
var all = FindObjectsByType<Card>(FindObjectsSortMode.None);
```

---

## 2. Player Settings — Resolution UI Changed

Unity 6's **Player Settings → Resolution and Presentation** no longer has the
**Default Screen Width / Height** numeric fields that existed in Unity 2022/2023.

The actual fields in Unity 6 (Windows/Mac/Linux Standalone):

- `Run In Background`
- **`Fullscreen Mode`** (Exclusive Fullscreen / Windowed / etc.)
- **`Default Is Native Resolution`** (checkbox — when on, the build launches at the monitor's native resolution)
- Standalone Player Options: Use Player Log, Resizable Window, Visible In Background, Allow Fullscreen Switch, Force Single Instance, Use DXGI flip model swapchain for D3D11

### Implication for this project
We want the game to run at **1920×1080**. Since Player Settings cannot specify a default resolution numerically anymore:

- **Do not** rely on Editor fields to lock 1920×1080 at launch.
- **Do** call `Screen.SetResolution(1920, 1080, FullScreenMode.ExclusiveFullScreen)` at runtime — this is already handled by `DisplayManager` in Stage 0.
- It is acceptable to leave `Default Is Native Resolution` on; `DisplayManager` overrides resolution shortly after launch.
- For windowed-mode development, also call `Screen.SetResolution(1920, 1080, FullScreenMode.Windowed)`.

**Editor instructions referencing Default Screen Width/Height fields are wrong for Unity 6 — ignore them.**

---

## 3. UI Framework Choice — UGUI (not UI Toolkit)

Unity 6 promotes UI Toolkit, but **this project uses UGUI** (Canvas + RectTransform + TMP).
- Do not generate UXML / USS / UI Toolkit code.
- Do not suggest converting UI to UI Toolkit.
- All UI is `UnityEngine.UI` + `TMPro`.

---

## 4. Input System — Use Legacy `Input` Class

This project uses the **legacy `UnityEngine.Input`** module (e.g., `Input.GetMouseButtonDown`, `Input.mousePosition`).
- Do not introduce `UnityEngine.InputSystem` (new Input System package) unless explicitly requested.
- Drag & drop in Stage 4 uses Unity's `EventSystem` + `IPointerXxxHandler` / `IDragHandler` interfaces, which are UGUI-native and do not require the new Input System.

---

## 5. Async — Prefer Coroutines, Not `Awaitable`

Unity 6 introduces `UnityEngine.Awaitable` for async/await-style code, but for this project:
- **Use coroutines** (`IEnumerator` + `StartCoroutine`) for animation, timers, and delays.
- Do not introduce `async`/`await` patterns or `Awaitable` types unless explicitly requested.
- Rationale: consistency across the codebase + easier for a 2-person team to read.

---

## 6. TextMeshPro — Still Use `TMPro` Namespace

Unity 6 keeps TMP under the `TMPro` namespace and its current API:
- `using TMPro;`
- `TextMeshProUGUI` for UGUI text components
- `TMP_FontAsset` for font assets

Do not introduce `UnityEngine.TextCore.Text` namespace types for normal UI work.

---

## 7. AudioMixer Exposed Parameters

Standard procedure (unchanged from older versions, but listed for clarity):

1. Open the mixer in the Audio Mixer window.
2. Right-click the Volume slider of a group in the Inspector → **Expose 'Volume (of X)' to script**.
3. In the **top-right of the Audio Mixer window**, find the "Exposed Parameters" dropdown.
4. Click the exposed parameter name → rename to a stable string (e.g., `MasterVolume`).
5. From script: `mixer.SetFloat("MasterVolume", dB);`

The volume value passed to `SetFloat` is in **dB**, not linear 0–1:
```csharp
float dB = (linear01 <= 0.0001f) ? -80f : Mathf.Log10(linear01) * 20f;
mixer.SetFloat("MasterVolume", dB);
```

---

## 8. Scene Loading — `SceneManager` API

Use `UnityEngine.SceneManagement.SceneManager.LoadScene(name)` for synchronous loads.
- Do **not** use legacy `Application.LoadLevel` (long removed).
- Async loads: `SceneManager.LoadSceneAsync(name)` returning an `AsyncOperation`.

---

## 9. Quit — `Application.Quit()` + Editor Guard

```csharp
public static void QuitGame()
{
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
}
```
- The Editor branch must use the field assignment `isPlaying = false` (NOT `isPlayingChanged`, which is an event, not a setter).

---

## 10. PlayerPrefs — bools via int

`PlayerPrefs` has no `SetBool`. Store as int:
```csharp
PlayerPrefs.SetInt("display_fullscreen", IsFullscreen ? 1 : 0);
PlayerPrefs.Save();

bool fullscreen = PlayerPrefs.GetInt("display_fullscreen", 1) == 1;
```
Always call `PlayerPrefs.Save()` after writes that should persist immediately (not just at app shutdown).

---

## 11. Serialization — `[SerializeField] private` over `public`

Convention for this project:
```csharp
[SerializeField] private Button startButton;   // ✅ visible in Inspector, encapsulated
public Button startButton;                     // ❌ avoid for Inspector-wired refs
```

---

## 12. Singleton Dedup Pattern

```csharp
public static AudioManager Instance { get; private set; }

private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
    // ... rest of init
}
```

---

## 13. Target Framerate

Set once in a manager's `Awake`:
```csharp
Application.targetFrameRate = 60;
```
- Do not rely on Quality Settings VSync alone — explicitly set the frame target.
- This is also fine to set inside `AudioManager.Awake` (a `DontDestroyOnLoad` singleton that loads first).

---

## 14. EventSystem Requirement

Every scene that has UI input (buttons, drag/drop) must contain an `EventSystem` GameObject.
- Add via GameObject menu → UI → Event System (or it's auto-added with the first Canvas).
- Without it, UI buttons and pointer events do nothing.

---

## Change Log (add entries as new pitfalls are found)

| Date | Item | Source |
|------|------|--------|
| 2026-05-17 | Initial doc — Find APIs, Player Settings resolution UI change, UGUI/Input/Async stance | User-reported (Player Settings) + Unity 6 docs |
