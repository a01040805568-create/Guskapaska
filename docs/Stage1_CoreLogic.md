# Stage 1 — Core Logic & Data Structures

---

## Before Starting (Workflow Checklist)

This section is for the human developer. Claude does not need to read it to generate code — it is for organizing work.
Full workflow details are in `03_Workflow.md`.

### Pre-flight checks
- [ ] `git checkout main && git pull` — your local `main` is up to date
- [ ] Previous stage (Stage 0) is fully merged and verified
- [ ] Claude Projects has the latest versions of `00_GameDesign.md`, `01_ProjectOverview.md`, `02_Unity6_Guidelines.md`, and this Stage doc
- [ ] You have read this Stage doc end-to-end and understand the deliverables
- [ ] Unity Test Runner is accessible (Window → General → Test Runner)

### Stage 1 Branch Breakdown

This stage is divided into **4 branches**. Each becomes a separate PR.

| # | Branch | Contents | Depends on |
|---|---|---|---|
| 1 | `feature/stage1-card-system` | `Card`, `CardShape`, `CardFactory` + tests | — |
| 2 | `feature/stage1-collections` | `Hand`, `Deck`, `RandomProvider` + tests | Branch 1 |
| 3 | `feature/stage1-round-resolution` | `RpsJudge`, `RoundOutcome`, `RoundResolver` + tests | Branch 1 |
| 4 | `feature/stage1-ai` | `AiRandomStrategy`, `AutoSubmitPicker` + tests | Branch 2 |

Branches 2 and 3 can be worked on in parallel after branch 1 is merged.

### Per-branch loop

```bash
# Start
git checkout main
git pull
git checkout -b feature/stage1-<name>

# Work + commit (small commits encouraged)
git add <files>
git commit -m "feat: <short description>"

# Push and open PR on GitHub
git push -u origin feature/stage1-<name>
# → create Pull Request to main, request review

# After merge
git checkout main
git pull
git branch -d feature/stage1-<name>
```

### PR Checklist (see `03_Workflow.md §6` for full list)
- [ ] All EditMode tests pass
- [ ] No compiler warnings
- [ ] No `Debug.Log` left from debugging
- [ ] Commit messages follow `<type>: <description>` format
- [ ] No deprecated Unity 6 APIs used

### Stage 1 complete when
- All 4 branches merged into `main`
- `Test Runner → EditMode → Run All` shows 100% pass
- No compiler warnings in Console

---

## Context
- Refer to `00_GameDesign.md` for all game rules.
- Refer to `01_ProjectOverview.md` for tech stack, namespaces, folder structure, and conventions.
- Refer to `02_Unity6_Guidelines.md` for Unity 6 specific rules — strictly follow it.
- Do not duplicate game rules or conventions in code comments; reference the docs instead.

## Stage Goal
Build the **pure game logic foundation**: cards, hands, deck, RPS judgment, coin calculator, and a round-state value object.
All classes in this stage are **plain C#** (no `MonoBehaviour`, no `UnityEngine` UI/visual dependencies). The only Unity reference allowed is `UnityEngine.Random` if needed — but prefer `System.Random` so logic is unit-testable from any context.

**Do NOT implement** in this stage:
- MonoBehaviour game managers (Stage 2)
- Any UI, prefab, or scene work (Stage 3+)
- Animations, drag-and-drop, audio (Stage 4–6)

---

## Deliverables

### 1. Scripts

All scripts live under `Assets/Scripts/Core/` and use namespace `Guskapaska.Core`.

#### `CardShape.cs`
```csharp
public enum CardShape
{
    Scissors,
    Rock,
    Paper
}
```
- XML doc comment on the enum noting it maps to 가위 / 바위 / 보자기 in UI.

#### `Card.cs`
```csharp
public class Card
{
    public string Id { get; }              // unique: "Card_S1_a", "Card_R0_b", "Card_P2_c" etc.
    public CardShape Shape { get; }
    public int CoinValue { get; }          // 0, 1, or 2

    public Card(string id, CardShape shape, int coinValue);

    public override string ToString();     // returns "Card_S1_a(Scissors,1)" for debugging
    public override bool Equals(object obj);
    public override int GetHashCode();
}
```
- Equality based on `Id` only (each card instance is unique by ID).
- `CoinValue` must be 0, 1, or 2 — throw `ArgumentOutOfRangeException` otherwise.
- Immutable: all properties have only getters.

#### `CardFactory.cs`
```csharp
public static class CardFactory
{
    /// <summary>
    /// Creates the canonical 12-card set defined in 00_GameDesign.md §2.
    /// Cards are returned in a stable order (Scissors → Rock → Paper, then by coin value).
    /// IDs are deterministic per call: "Card_{ShapeLetter}{CoinValue}_{a|b|c}".
    /// </summary>
    public static List<Card> CreateStandardSet();
}
```

ID scheme:
- Shape letter: S / R / P
- Coin value: 0 / 1 / 2
- Suffix `a`, `b`, `c` to distinguish duplicates of the same (shape, coin) pair

The 12 cards must be exactly:
- `Card_S0_a` (Scissors, 0)
- `Card_S1_a`, `Card_S1_b`, `Card_S1_c` (Scissors, 1 — three copies)
- `Card_S2_a`, `Card_S2_b` (Scissors, 2 — two copies)
- `Card_R0_a`, `Card_R1_a`, `Card_R2_a` (Rock, 0/1/2)
- `Card_P0_a`, `Card_P1_a`, `Card_P2_a` (Paper, 0/1/2)

This matches the table in `00_GameDesign.md §2`: 12 cards total, 13 coins total.

#### `RpsJudge.cs`
```csharp
public enum RpsResult
{
    Draw,
    LeftWins,
    RightWins
}

public static class RpsJudge
{
    /// <summary>
    /// Standard rock-paper-scissors comparison.
    /// Scissors beats Paper, Paper beats Rock, Rock beats Scissors.
    /// </summary>
    public static RpsResult Compare(CardShape left, CardShape right);
}
```

#### `Hand.cs`
```csharp
public class Hand
{
    public IReadOnlyList<Card> Cards { get; }
    public int Count { get; }
    public bool IsEmpty { get; }

    public Hand();
    public Hand(IEnumerable<Card> initial);

    public void Add(Card card);
    public void AddRange(IEnumerable<Card> cards);
    public bool Remove(Card card);                 // by Id equality
    public bool Contains(Card card);
    public Card GetAt(int index);
    public void Clear();
}
```
- Internally backed by `List<Card>`.
- `Cards` returns a read-only view (do not allow external mutation).

#### `Deck.cs`
```csharp
public class Deck
{
    public int Count { get; }
    public bool IsEmpty { get; }

    public Deck(IEnumerable<Card> cards);

    /// <summary>
    /// In-place Fisher-Yates shuffle using the supplied RNG.
    /// If rng is null, a new System.Random with a time-based seed is created internally.
    /// </summary>
    public void Shuffle(System.Random rng = null);

    public Card DrawTop();                         // throws if empty
    public List<Card> DrawTop(int count);          // throws if count > Count
    public IReadOnlyList<Card> Peek();             // read-only view of remaining cards
}
```

#### `RandomProvider.cs`
```csharp
public static class RandomProvider
{
    /// <summary>
    /// Default RNG. Re-seedable for tests/reproduction.
    /// </summary>
    public static System.Random Default { get; private set; } = new System.Random();

    public static void Seed(int seed);             // replaces Default with new System.Random(seed)
    public static void Reset();                    // replaces Default with a fresh time-seeded instance
}
```
Used by AI random card selection, deck shuffling, and timer-expiry auto-submit. Centralizing makes test reproducibility trivial.

#### `RoundOutcome.cs`
```csharp
public enum RoundWinner
{
    None,        // draw
    Player,
    Ai
}

public class RoundOutcome
{
    public RoundWinner Winner { get; }
    public Card PlayerCard { get; }
    public Card AiCard { get; }
    public int CoinsAwarded { get; }               // 0 if draw
    public int DrawCoinsBefore { get; }            // accumulator value entering this round
    public int DrawCoinsAfter { get; }             // accumulator value after this round
    public IReadOnlyList<Card> CardsTransferredToLoser { get; }  // empty on draw

    public RoundOutcome(
        RoundWinner winner,
        Card playerCard,
        Card aiCard,
        int coinsAwarded,
        int drawCoinsBefore,
        int drawCoinsAfter,
        IReadOnlyList<Card> cardsTransferredToLoser);
}
```
Immutable value object describing what happened in one round. Stage 2 will produce these from a `GameManager` and feed them to UI for animation/feedback.

#### `RoundResolver.cs`
```csharp
public static class RoundResolver
{
    /// <summary>
    /// Pure function: given submitted cards, the accumulated draw pot,
    /// and the current draw-coin accumulator, computes the outcome.
    /// Does NOT mutate hands — that is the caller's responsibility based on the outcome.
    /// </summary>
    /// <param name="playerCard">Card submitted by the player this round.</param>
    /// <param name="aiCard">Card submitted by the AI this round.</param>
    /// <param name="accumulatedDrawCards">Cards stashed from previous draws (may be empty).</param>
    /// <param name="accumulatedDrawCoins">Coin counter from previous draws.</param>
    public static RoundOutcome Resolve(
        Card playerCard,
        Card aiCard,
        IReadOnlyList<Card> accumulatedDrawCards,
        int accumulatedDrawCoins);
}
```

Resolution rules (from `00_GameDesign.md §5.3`):
- **Draw** (same shape):
  - `Winner = None`
  - `CoinsAwarded = 0`
  - `DrawCoinsAfter = DrawCoinsBefore + playerCard.CoinValue + aiCard.CoinValue`
  - `CardsTransferredToLoser = []`
- **Player wins**:
  - `Winner = Player`
  - `CoinsAwarded = playerCard.CoinValue + aiCard.CoinValue + DrawCoinsBefore`
  - `DrawCoinsAfter = 0`
  - `CardsTransferredToLoser = accumulatedDrawCards ∪ {playerCard, aiCard}` — these will be added to AI's hand by the caller
- **AI wins**: symmetric, `CardsTransferredToLoser` will be added to Player's hand

#### `AiRandomStrategy.cs`
```csharp
public class AiRandomStrategy
{
    private readonly System.Random _rng;

    public AiRandomStrategy(System.Random rng = null);

    /// <summary>
    /// Selects a random card from the AI's hand. Throws if hand is empty.
    /// </summary>
    public Card SelectCard(Hand aiHand);
}
```
Stage 1 implementation is pure random. Future strategies (counting, difficulty) can implement a common interface later — but do not introduce an interface in Stage 1.

#### `AutoSubmitPicker.cs`
```csharp
public static class AutoSubmitPicker
{
    /// <summary>
    /// Picks a random card from the player's hand when the 15-second timer expires.
    /// </summary>
    public static Card PickRandom(Hand playerHand, System.Random rng = null);
}
```

---

### 2. Unit Tests

Create under `Assets/Tests/EditMode/Core/` with `Tests.EditMode.asmdef` referencing `nunit.framework.dll` and the Core scripts.

Required test files:

#### `CardFactoryTests.cs`
- Creates standard set → asserts count == 12.
- Asserts total coin value sum == 13.
- Asserts shape counts: 6 Scissors, 3 Rock, 3 Paper.
- Asserts coin-value counts for Scissors: 1×0, 3×1, 2×2.
- Asserts all card IDs are unique.

#### `RpsJudgeTests.cs`
- Scissors vs Paper → LeftWins
- Paper vs Rock → LeftWins
- Rock vs Scissors → LeftWins
- Paper vs Scissors → RightWins
- Rock vs Paper → RightWins
- Scissors vs Rock → RightWins
- Same shape (all 3 cases) → Draw

#### `DeckTests.cs`
- Shuffle with same seed produces same order (use `new System.Random(42)`).
- Shuffle with different seeds produces different orders (statistical, but two seeded shuffles of a 12-card deck should differ).
- DrawTop reduces count by 1.
- DrawTop(N) returns N cards in order.
- DrawTop on empty deck throws `InvalidOperationException`.

#### `HandTests.cs`
- Add, Remove, Contains by Id equality.
- Remove of card not in hand returns false.
- AddRange adds all.
- IsEmpty/Count reflect state correctly.

#### `RoundResolverTests.cs`
Cover every scenario from `00_GameDesign.md §8` plus edge cases:

1. **Simple win, no draw pot**: Player Scissors-1 vs AI Paper-2 → Player wins 3 coins, both cards move to AI hand pile.
2. **Zero-coin win**: Player Scissors-0 vs AI Paper-0 → Player wins 0 coins, cards still transfer.
3. **Win after accumulated draws**: drawPot = 4, drawCards = [c1, c2, c3, c4]; Player Scissors-1 vs AI Paper-2 → Player wins 1+2+4 = 7 coins, all 6 cards (4 stash + 2 played) transfer to AI.
4. **Draw resets nothing**: drawPot = 4 entering; Player Rock-2 vs AI Rock-1 → outcome Winner=None, CoinsAwarded=0, DrawCoinsAfter = 4+2+1 = 7.
5. **AI wins symmetric**: Player Rock-0 vs AI Paper-2 → AI wins 2 coins, both cards to Player hand.

#### `AiRandomStrategyTests.cs`
- With seeded `System.Random`, SelectCard returns deterministic card.
- SelectCard on empty hand throws.
- Across 1000 iterations on a hand of 5 distinct cards, each card is selected at least once (basic randomness sanity).

---

### 3. Folder Structure After Stage 1

```
Assets/
├── Scripts/
│   └── Core/
│       ├── AiRandomStrategy.cs
│       ├── AutoSubmitPicker.cs
│       ├── Card.cs
│       ├── CardFactory.cs
│       ├── CardShape.cs
│       ├── Deck.cs
│       ├── Hand.cs
│       ├── RandomProvider.cs
│       ├── RoundOutcome.cs
│       ├── RoundResolver.cs
│       └── RpsJudge.cs
└── Tests/
    └── EditMode/
        ├── Tests.EditMode.asmdef
        └── Core/
            ├── AiRandomStrategyTests.cs
            ├── CardFactoryTests.cs
            ├── DeckTests.cs
            ├── HandTests.cs
            ├── RoundResolverTests.cs
            └── RpsJudgeTests.cs
```

`Tests.EditMode.asmdef` must include references to `UnityEngine.TestRunner` and `UnityEditor.TestRunner` and have platform set to **Editor only**, plus `nunit.framework.dll` precompiled reference.

---

## Output Format (what the AI should produce)

1. **File tree**: every new file with full path under `Assets/`.
2. **Source code**: complete contents of every `.cs` file, including all tests.
3. **`.asmdef` content**: full JSON for `Tests.EditMode.asmdef`.
4. **Verification steps**:
   - Open Unity → Window → General → Test Runner → EditMode tab.
   - Click **Run All** → all tests should pass.
   - In code, instantiate `CardFactory.CreateStandardSet()` and confirm Console output via a temporary `Debug.Log` (optional).

---

## Constraints

- **No `MonoBehaviour`** in any Core script.
- **No `UnityEngine.GameObject` / `UnityEngine.UI` / `Transform`** references.
- `UnityEngine.Random` is **discouraged** — use `System.Random` (via `RandomProvider` or injected) so logic is testable from EditMode.
- All public methods documented with English XML comments.
- All classes immutable where reasonable (Card, RoundOutcome).
- Throw clear, specific exceptions on invalid input (`ArgumentOutOfRangeException`, `InvalidOperationException`, etc.) rather than silent failure.
- Do not write any code that touches UI, prefabs, scenes, or animations — those belong to later stages.
