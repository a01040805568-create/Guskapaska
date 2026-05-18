# 03. Development Workflow

This document describes how to organize work, manage branches, and update documentation across the project.
**This is a human-facing document** — it is not referenced inside Stage prompts (Claude doesn't need to read it to generate code).

---

## 1. Repository Layout

```
guskapaska/                       (repo root)
├── Assets/                       (Unity project)
├── Packages/
├── ProjectSettings/
├── docs/                         (all .md design docs)
│   ├── 00_GameDesign.md
│   ├── 01_ProjectOverview.md
│   ├── 02_Unity6_Guidelines.md
│   ├── 03_Workflow.md            (this file)
│   ├── Stage0_MainMenu.md
│   ├── Stage1_CoreLogic.md
│   └── ...
├── .gitignore
└── README.md
```

---

## 2. Branch Strategy

### Core Rule
**One task = one branch = one Pull Request.**
Never use long-lived per-person branches (`alice-branch`, `bob-branch`).

### Branch Naming

```
feature/<stage>-<short-name>     New feature work
fix/<short-name>                 Bug fix
docs/<short-name>                Larger doc work (small edits go to main directly)
refactor/<short-name>            Refactoring
```

Rules:
- lowercase, kebab-case
- specific and short (3–5 words)
- include stage prefix for stage work (`stage1-`, `stage2-`, …)

**Good:** `feature/stage1-card-system`, `fix/card-shuffle-bug`, `docs/stage-3`
**Bad:** `feature/work`, `alice-stage1`, `feature/Stage1_Card_System`

### Branch Size
| Limit | Target |
|---|---|
| Time | 0.5 – 2 days of work |
| Files changed | usually 1 – 10 |
| One-line description | "Add Hand and Deck" ✅ / "Stage 1 everything" ❌ |
| Independently mergeable? | yes — main must not break if this is the only thing merged |

---

## 3. When to Branch vs Push to `main`

### Push to `main` directly (no branch)
- Small doc fixes (typo, single section update)
- Adding a new Stage doc that doesn't change existing files
- Updating `02_Unity6_Guidelines.md` change log

### Create a branch + PR
- All code work
- Larger doc rewrites
- Anything touching scenes (`.unity`) or prefabs (`.prefab`) — merge conflicts here are painful
- Changes to `00_GameDesign.md` (game rules — affects teammate's work)

---

## 4. Standard Workflow (per task)

```bash
# 1. Sync main
git checkout main
git pull

# 2. Create branch
git checkout -b feature/stage1-card-system

# 3. Work + commit frequently
git add Assets/Scripts/Core/Card.cs
git commit -m "feat: add Card class"

git add Assets/Scripts/Core/CardFactory.cs
git commit -m "feat: add CardFactory with standard 12-card set"

# 4. Push and create PR
git push -u origin feature/stage1-card-system
# → On GitHub, open Pull Request to main, request review

# 5. After review approval, merge via GitHub UI

# 6. Local cleanup
git checkout main
git pull
git branch -d feature/stage1-card-system
```

---

## 5. Commit Message Convention

Format: `<type>: <short description>`

| Prefix | Meaning |
|---|---|
| `feat:` | New feature |
| `fix:` | Bug fix |
| `refactor:` | Code restructure, no behavior change |
| `docs:` | Documentation only |
| `test:` | Adding or updating tests |
| `chore:` | Tooling, gitignore, project settings |

Keep messages under ~70 characters. Use imperative mood ("add" not "added").

Examples:
- `feat: add Card class with unique ID system`
- `fix: correct CoinValue validation in Card constructor`
- `docs: clarify draw accumulator rule in game design`
- `test: add deck shuffle determinism test`

---

## 6. Pull Request Checklist

Before opening or merging a PR, verify:

- [ ] `main` is merged into your branch (no conflicts pending)
- [ ] All unit tests pass (Unity Test Runner → EditMode + PlayMode if applicable)
- [ ] No compiler warnings in Console
- [ ] Code follows conventions in `01_ProjectOverview.md`
- [ ] Code follows Unity 6 rules in `02_Unity6_Guidelines.md` (no deprecated APIs)
- [ ] Korean text matches `00_GameDesign.md` terminology (가위 / 바위 / 보자기, 코인, 보석 etc.)
- [ ] No `Debug.Log` left from active debugging
- [ ] No unused `using` statements
- [ ] Commit messages follow §5 convention

---

## 7. Updating Documentation in GitHub

### Small change (typo, single guideline entry)
```bash
git pull
# edit file
git add docs/02_Unity6_Guidelines.md
git commit -m "docs: add Unity 6 pitfall - X"
git push
```

### Larger change (new Stage, rule change)
Use a branch + PR (see §4).

### Important: Claude Projects does NOT auto-sync with GitHub
After updating docs in GitHub, you must manually refresh Claude Projects:

1. Delete the outdated file from Claude Projects
2. Download the updated file from GitHub (or use your local copy)
3. Upload the new file to Claude Projects

Keep `02_Unity6_Guidelines.md` especially up to date in Projects, since Claude relies on it most.

---

## 8. Parallel Work Coordination (2-person team)

### Default split for stage work
When a stage has multiple branches, one person can take all of them sequentially, or two people split by branch.

### Avoiding scene/prefab conflicts
- Edit prefabs over scenes when possible
- If both people might touch the same scene, coordinate in advance — finish one branch before starting another that edits the same `.unity` file
- Enable Editor → Project Settings → Editor:
  - **Asset Serialization Mode: Force Text**
  - **Version Control Mode: Visible Meta Files**
  (already set in `01_ProjectOverview.md` §7 but worth re-checking)

### Reviewing teammate's PR
Lightweight rules (suggested):
| PR size | Review depth |
|---|---|
| < 50 lines | Quick scan, comment if needed |
| 50 – 300 lines | 10-minute review, run tests locally if substantive |
| > 300 lines | Detailed review, consider asking for the PR to be split |

---

## 9. Per-Stage Workflow (the pattern)

Every stage follows this loop:

1. **Read** `StageN_xxx.md` — understand deliverables and scope
2. **Plan** branch breakdown (see "Branches" section in each Stage doc)
3. **Execute** branches sequentially or in parallel, one PR per branch
4. **Verify** by running Test Runner / Play Mode tests as listed in the Stage doc
5. **Update** Claude Projects with any docs that changed during the stage
6. **Mark complete** — optionally add a note to a CHANGELOG or just confirm via merged PRs

The next stage starts only after the previous stage's PRs are all merged and verified.

---

## 10. Useful Git Commands

```bash
# See current branch and status
git status

# See branches (local)
git branch

# See branches (remote)
git branch -r

# Delete a local branch (after merge)
git branch -d feature/stage1-card-system

# Delete a remote branch (after merge)
git push origin --delete feature/stage1-card-system

# Pull latest main into your feature branch (resolve conflicts as you go)
git checkout feature/stage1-card-system
git pull origin main

# Discard local changes to a file
git checkout -- path/to/file.cs

# See log of recent commits
git log --oneline -20
```

---

## 11. Stage Branch Reference

A quick lookup for which branches each stage breaks into.
Branch lists live inside each Stage doc's "Before Starting" section — this is just an index.

| Stage | File | Branch Count (suggested) |
|---|---|---|
| 0 | `Stage0_MainMenu.md` | 1 (single branch OK due to small scope) |
| 1 | `Stage1_CoreLogic.md` | 4 (card-system, collections, round-resolution, ai) |
| 2 | `Stage2_GameManager.md` | TBD |
| 3 | `Stage3_UI.md` | TBD |
| 4 | `Stage4_DragDrop.md` | TBD |
| 5 | `Stage5_Animation.md` | TBD |
| 6 | `Stage6_Polish.md` | TBD |
| 7 | `Stage7_Tutorial.md` | TBD |
