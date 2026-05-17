# 00. Game Design — 구스카파스카

This document is the **single source of truth** for game rules and design.
All Stage prompts reference this document. If a rule changes, update only this file.

---

## 1. Game Identity

- **Title:** 구스카파스카 (Guskapaska)
- **Genre:** Rock-Paper-Scissors card game with coin/gem betting mechanics
- **Players:** 1 (Player) vs 1 (AI)
- **Match Length:** Variable (ends when center gem pile empties or one side has 0 cards)

---

## 2. Card Composition (Total: 12 cards)

| Type | Coin 0 | Coin 1 | Coin 2 |
|------|--------|--------|--------|
| ✌️ Scissors (가위)  | 1 | 3 | 2 |
| ✊ Rock (바위)       | 1 | 1 | 1 |
| 🖐️ Paper (보자기)  | 1 | 1 | 1 |

- Total cards: **12**
- Total coins printed across all cards: **13**
- Each card has two attributes: `Shape` (Scissors / Rock / Paper) and `CoinValue` (0 / 1 / 2)
- Card visuals: 3 designs (one per shape), with coin count rendered on the card face

---

## 3. Center Gem Pile

- Starts at **13 gems**
- Gems are awarded to the round winner based on coin value (see §5)
- Game ends when this pile reaches **0** (or when a hand reaches 0)

---

## 4. Setup

1. Shuffle all 12 cards
2. Deal **5 cards** face-down to Player
3. Deal **5 cards** face-down to AI
4. Remaining **2 cards** are removed from play (flown off-screen during deal animation)
5. Player's hand is revealed face-up; AI's hand stays face-down
6. Center pile initialized to 13 gems
7. **Total cards always in play = 10** (Player hand + AI hand + center accumulated cards = 10 at all times)

---

## 5. Round Flow

### 5.1 Submission Phase (15-second timer)
- Both Player and AI simultaneously play 1 card from hand
- Player: drag-and-drop a card from hand into the submission area
- AI: randomly selects 1 card from its hand
- If Player does not submit within 15 seconds, a random card from Player's hand is auto-submitted
- A 3-2-1 countdown displays at screen center when 3 seconds remain

### 5.2 Reveal & Resolution
- Both cards are flipped face-up simultaneously
- RPS judgment is applied:
  - Scissors beats Paper
  - Paper beats Rock
  - Rock beats Scissors
  - Same shape = Draw

### 5.3 Outcome Handling

**Win (one side beats the other):**
- Winner takes gems from center pile equal to:
  `winner_card.coin + loser_card.coin + accumulated_draw_coins`
- Both played cards (winner's + loser's) go into the **loser's hand**
- Any accumulated draw cards from previous rounds also go into the **loser's hand**
- Accumulated draw coin counter resets to 0

**Draw (same shape):**
- Both cards are moved to a **center stash** (visible to both players)
- The sum of both cards' coin values is added to **accumulated draw coins**
- The next non-draw round's winner collects all accumulated cards & coins
- Hands shrink temporarily until the next decisive round

### 5.4 Card Conservation Rule
**The total number of cards in play is always 10.**
- Player hand size + AI hand size + center stash size = 10
- Cards never leave the game except via the initial 2-card discard at setup
- Gems can leave the game (taken from center pile)

---

## 6. AI Behavior

- **Stage 1 (current):** Fully random selection from AI's current hand
- No card counting, no probability-based decisions
- AI submission is instantaneous each round (no artificial delay in logic; visual animation handles pacing)

---

## 7. Game End Conditions

The game ends as soon as **either** condition is met:

1. **Center gem pile reaches 0** (all gems have been claimed)
2. **Either player's hand reaches 0 cards**

### Winner Determination
- The player holding **more gems** at end-of-game wins
- Tie in gems = draw (rare; handle as draw screen)

---

## 8. Coin / Gem Calculation Examples

| Scenario | Winner Gain |
|----------|-------------|
| Win: my coin-1 vs opponent coin-2 | 3 gems |
| Win: my coin-0 vs opponent coin-0 | 0 gems (cards still move to loser hand) |
| Win after 2 draws (draw1: 1+1=2, draw2: 2+0=2 → accumulated=4), with own coin-1 vs opp coin-2 | 1 + 2 + 4 = 7 gems |
| Draw | 0 gems gained; coins added to accumulator |

---

## 9. UI / Visual Behavior Summary

(Detailed implementation in Stage 3–5 docs; this is the design intent.)

- **Hand layout:** Fan-shape, right card overlapping left
- **Hover:** Card raises slightly, yellow highlight glow
- **Drag:** Card scales to 1.2×, rotates to upright orientation
- **Drop fail:** Smooth return to original slot (position/angle/scale)
- **Submit:** Player card shrinks and slides to submission zone; AI card flies in a parabolic arc from opponent deck
- **Coin grid (left side):** 13 cells in a grid; cells empty when gems are won
- **Gem-win animation:** Empty cells emit gems that fly upward and vanish; a "Mario-style" hand graphic comes down from a brown panel, covers the gems, lifts and disappears
- **Drop animation:** Gems drop one-by-one into the player's collection area from the left coin zone
- **Draw accumulator:** Shown as `coin_icon × N` on screen; activates on draw, deactivates on decisive round
- **Timer:** 15-second countdown shown to the right of timer text; large 3-2-1 overlay at center under 3 seconds

---

## 10. Out of Scope (for v1)

- Multiplayer / online play
- Card customization or unlocks
- Multiple AI difficulty levels
- Save/resume mid-game
- Languages other than Korean
