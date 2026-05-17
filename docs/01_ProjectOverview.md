# 01. Project Overview — 구스카파스카

This document describes the technical setup, folder structure, conventions, and stage breakdown for the project.
All Stage prompts assume the conventions defined here.

---

## 1. Technical Stack

| Item | Value |
|------|-------|
| Engine | Unity 6.3 LTS (6000.3.15f1) |
| Render Pipeline | Built-in 2D |
| UI Framework | UGUI (Canvas-based) |
| Text | TextMeshPro (TMP) |
| Target Resolution | 1920×1080 |
| Orientation | Landscape (fixed) |
| Display Modes | Fullscreen / Windowed 1920×1080 (toggleable in settings) |
| Target Framerate | 60 FPS |
| Language (in-game) | Korean only |
| Persistence | PlayerPrefs |
| Animation | Coroutine-based (no DOTween dependency unless explicitly requested) |
| External Packages | None beyond default Unity + TMP |

---

## 2. Code Conventions

- **Root namespace:** `Guskapaska`
- **Sub-namespaces:**
  - `Guskapaska.Core` — pure data/logic (no UnityEngine dependencies where possible)
  - `Guskapaska.Game` — gameplay managers (MonoBehaviour)
  - `Guskapaska.UI` — UI controllers
  - `Guskapaska.Audio` — audio system
  - `Guskapaska.Util` — helpers, extensions
- **File naming:** PascalCase matching class name (e.g., `CardData.cs`)
- **Public members:** XML doc comments in **English**
- **In-game string literals:** Korean (e.g., `"게임 시작"`)
- **Constants:** `UPPER_SNAKE_CASE` for true constants, `PascalCase` for readonly fields
- **Serialized fields:** `[SerializeField] private` with `_camelCase` is acceptable but match existing style consistently
- **Singletons:** `public static T Instance { get; private set; }` pattern with `DontDestroyOnLoad`
- **No magic numbers in code** — extract to named constants or serialized fields

---

## 3. Folder Structure

```
Assets/
├── Audio/
│   ├── MainMixer.mixer
│   ├── BGM/
│   └── SFX/
├── Fonts/
│   └── (Korean TMP font asset, e.g. NotoSansKR)
├── Prefabs/
│   ├── Cards/
│   ├── UI/
│   └── Effects/
├── Scenes/
│   ├── MainMenu.unity
│   └── Game.unity
├── Scripts/
│   ├── Core/
│   ├── Game/
│   ├── UI/
│   ├── Audio/
│   └── Util/
├── Sprites/
│   ├── Cards/
│   ├── UI/
│   └── Effects/
└── Settings/
    └── (project-level config assets if any)
```

---

## 4. Stage Breakdown

| Stage | Title | Scope | Depends On |
|-------|-------|-------|------------|
| 0 | Main Menu & Scene Infrastructure | Menu UI, settings panel, scene loader, audio/display managers | — |
| 1 | Core Logic & Data Structures | Card, Hand, Deck, RPS judgment, coin calculator (pure C#) | 0 |
| 2 | Game Manager & Round Flow | Turn cycle, win/lose/draw handling, timer, end conditions | 0, 1 |
| 3 | UI Base Layout | Hand fan layout, coin grid, gem pile, submission zone | 0, 1, 2 |
| 4 | Drag & Drop | Card hover, drag, drop, submit, return-to-hand | 3 |
| 5 | Animation | Deal animation, AI parabolic submit, coin/gem motion, hand graphic, 3-2-1 countdown | 4 |
| 6 | Polish | Sound integration, result screen, design slot for card art | 5 |
| 7 | Tutorial | Interactive tutorial reusing game systems | All prior |

Each stage produces working, testable code before the next begins.

---

## 5. Persistence Keys (PlayerPrefs)

Reserved keys (do not reuse):

| Key | Type | Default | Purpose |
|-----|------|---------|---------|
| `vol_master` | float | 1.0 | Master volume (linear 0–1) |
| `vol_bgm` | float | 0.8 | BGM volume (linear 0–1) |
| `vol_sfx` | float | 1.0 | SFX volume (linear 0–1) |
| `display_fullscreen` | int | 1 | 1 = fullscreen, 0 = windowed |
| `has_played_before` | int | 0 | 1 after first game completed, used to suggest tutorial |

---

## 6. Scene List & Build Index

| Index | Scene |
|-------|-------|
| 0 | MainMenu |
| 1 | Game |

---

## 7. Collaboration Notes (2-person team via GitHub)

- Use `.gitignore` for Unity (Library/, Temp/, Logs/, etc.)
- Enable **Visible Meta Files** and **Force Text** serialization in Editor → Project Settings → Editor
- Prefer prefab edits over scene edits to minimize merge conflicts
- Suggested branch split:
  - Person A: Core/Game/Audio scripts
  - Person B: UI/Animation/Scene assembly
- Stage 0 should be done together (sets shared foundation)

---

## 8. Asset Placeholders

- Card art will be added **after** mechanics are complete. Until then, use plain colored rectangles with TMP text showing shape + coin count.
- Sound effects can use Unity built-in placeholders or be left as empty `AudioClip` slots (AudioManager API will be wired up; clips assigned later).

---

## 9. How Stage Prompts Reference These Docs

Each Stage prompt begins with:
```
# Context
- Refer to `00_GameDesign.md` for all game rules.
- Refer to `01_ProjectOverview.md` for tech stack, namespaces, folder structure, and conventions.
- Do not duplicate game rules or conventions in code comments; reference the docs instead.
```

This avoids redundancy and keeps a single source of truth.
