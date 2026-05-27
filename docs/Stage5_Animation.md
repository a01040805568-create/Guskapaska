# Stage 5 — Animation

---

## Before Starting (Workflow Checklist)

이 섹션은 개발자용이며 Claude가 코드 생성 시 읽을 필요는 없습니다.
워크플로우 상세는 `03_Workflow.md` 참조.

### Pre-flight checks
- [ ] `git checkout main && git pull` — 로컬 `main`이 최신
- [ ] Stage 4가 전부 머지됨 (4개 브랜치 모두)
- [ ] Play 모드에서 드래그-드롭 카드 제출이 정상 동작
- [ ] EditMode + PlayMode 테스트 100% 통과
- [ ] Claude Projects에 `00_GameDesign.md`, `01_ProjectOverview.md`, `02_Unity6_Guidelines.md`, 그리고 이 Stage 문서가 최신 상태
- [ ] **Stage 4에서 미뤘던 시각 이슈 메모 확인**: PlayerSlot이 빈 상태일 때 보라색으로 보이는 문제 → Branch 1에서 처리

### Stage 5 Branch Breakdown

이 Stage는 **5개 브랜치**로 분할. 각각 별도 PR.

| # | Branch | Contents | Depends on |
|---|---|---|---|
| 1 | `feature/stage5-tween-foundation` | `TweenRunner` 유틸리티(코루틴 기반 트윈), Easing 함수, PlayerSlot 빈 상태 시각 처리 | — |
| 2 | `feature/stage5-card-motion` | 드롭 실패 시 부드러운 복귀, 호버/드래그 트윈, 플레이어 카드 제출 슬라이드 | Branch 1 |
| 3 | `feature/stage5-ai-submit-arc` | AI 카드 포물선 비행 애니메이션, 카드 딜(deal) 애니메이션 | Branch 1 |
| 4 | `feature/stage5-gem-fx` | 보석 비행 애니메이션(빈 셀→플레이어/AI 보석 더미), 마리오 스타일 손 그래픽 | Branch 1 |
| 5 | `feature/stage5-countdown-overlay` | 3-2-1 카운트다운 오버레이, 부채꼴 손패 레이아웃 활성화 | Branch 1 |

> Branches 2, 3, 4, 5는 Branch 1 머지 후 **병렬 작업 가능** — 서로 다른 파일/영역을 다룸.

### Per-branch loop

```bash
git checkout main
git pull
git checkout -b feature/stage5-<name>

# 작업 + 커밋
git add <files>
git commit -m "feat: <short description>"

git push -u origin feature/stage5-<name>
# → GitHub에서 PR 생성, Squash and merge

# 머지 후
git checkout main
git pull
git branch -d feature/stage5-<name>
```

### PR Checklist
- [ ] 모든 기존 EditMode/PlayMode 테스트 통과 (회귀 없음)
- [ ] 컴파일 경고 0개
- [ ] `Debug.Log` 잔존 없음
- [ ] Unity 6 deprecated API 사용 없음
- [ ] 한국어 텍스트는 `00_GameDesign.md` 용어와 일치
- [ ] 메서드 본문 안의 주석은 한국어, 메서드 위의 XML 문서 주석은 영어
- [ ] 애니메이션 진행 중 사용자 입력이 깨지지 않음 (예: 드래그 도중 라운드 종료 시 카드 복귀 정상)

### Stage 5 완료 조건
- 5개 브랜치 모두 main에 머지됨
- Game 씬 Play → 다음이 모두 동작:
  - 매치 시작 시 카드가 부드럽게 딜됨
  - 카드 호버/드래그가 즉각 점프 없이 부드럽게 변화
  - 플레이어 카드 제출 시 PlayerSlot으로 부드럽게 이동
  - AI 카드 제출 시 포물선 궤적으로 AiSlot으로 비행
  - 라운드 종료 후 보석이 플레이어/AI 보석 더미로 비행 + 손 그래픽
  - 타이머 3초 이하일 때 화면 중앙에 큰 3-2-1 카운트다운 오버레이
  - 손패가 부채꼴로 배치됨 (HandView.arcAngleDegrees > 0)
- EditMode + PlayMode 회귀 테스트 100% 통과
- 컴파일 경고 0개

---

## Context
- `00_GameDesign.md` §9 — UI/Visual Behavior Summary (애니메이션 디자인 의도)
- `01_ProjectOverview.md` — 네임스페이스, 폴더 구조, 컨벤션
- `02_Unity6_Guidelines.md` — Unity 6 규칙 (특히 §11 Coroutines)
- `Stage4_DragDrop.md` — Stage 4 결과물(CardInteractable, DragController 등)
- 게임 규칙이나 컨벤션을 코드 주석에 중복하지 않음. 문서를 참조할 것

## Stage Goal
Stage 3, 4에서 즉시(snap) 처리되던 모든 시각적 전환을 **부드러운 애니메이션**으로 교체. 또한 미뤄두었던 시각 디테일을 모두 추가:

- 카드 이동(제출, 복귀, 딜) → 트윈 보간
- AI 카드 → 포물선 비행
- 보석 → 빈 셀에서 솟구쳐 보석 더미로 비행
- 손패 → 부채꼴 배치 활성화
- 타이머 3초 이하 → 화면 중앙 카운트다운 오버레이
- "마리오 스타일 손 그래픽" → 보석 획득 연출

**기술적 결정 (사전 합의)**:
- **외부 트윈 라이브러리(DOTween 등) 사용 안 함** — Unity 기본 Coroutine + AnimationCurve로 자체 구현. `02_Unity6_Guidelines.md §11`과 일관성 유지
- **시간 단위는 초(float)** — 모든 애니메이션 지속 시간은 `f` 접미사가 붙은 float
- **모든 트윈은 취소 가능** — 새 애니메이션이 시작되면 이전 트윈을 중단

**다음은 이번 단계 범위에 포함되지 않음:**
- 사운드 (Stage 6)
- 카드 아트(실제 일러스트) — 여전히 색상+텍스트 placeholder (Stage 6)
- 튜토리얼 (Stage 7)

---

## Deliverables

### 1. Scripts

모든 스크립트는 `Assets/Scripts/UI/` 또는 `Assets/Scripts/Util/` 아래, 네임스페이스 `Guskapaska.UI` 또는 `Guskapaska.Util`.

---

#### `TweenRunner.cs` — namespace `Guskapaska.Util` (Branch 1)

코루틴 기반 트윈 헬퍼. 모든 애니메이션이 이를 통해 진행됨.

```csharp
public static class TweenRunner
{
    /// <summary>Run a coroutine on the given MonoBehaviour, cancelling any previous tween with the same key.</summary>
    public static Coroutine Run(MonoBehaviour host, string key, IEnumerator routine);

    /// <summary>Cancel a tween started with Run.</summary>
    public static void Cancel(MonoBehaviour host, string key);

    /// <summary>Cancel all tweens on this host.</summary>
    public static void CancelAll(MonoBehaviour host);

    // 자주 쓰이는 트윈 헬퍼들
    public static IEnumerator MoveLocal(Transform t, Vector3 from, Vector3 to, float duration, AnimationCurve curve);
    public static IEnumerator MoveLocalArc(Transform t, Vector3 from, Vector3 to, float arcHeight, float duration, AnimationCurve curve);
    public static IEnumerator Scale(Transform t, Vector3 from, Vector3 to, float duration, AnimationCurve curve);
    public static IEnumerator Rotate(Transform t, Quaternion from, Quaternion to, float duration, AnimationCurve curve);
    public static IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, AnimationCurve curve);
}
```

- `key` 기반 취소 메커니즘: 같은 카드에 두 번 연속 트윈이 시작되면 첫 번째가 즉시 중단. 새 트윈 시작 시점부터 새 값으로 진행
- `MoveLocalArc` — 포물선 이동. AI 카드 제출 시 사용
- 모든 헬퍼는 마지막에 정확히 `to` 값으로 끝남 (부동소수점 오차 방지)
- AnimationCurve가 null이면 EaseOutQuad 기본 적용

---

#### `EasingCurves.cs` — namespace `Guskapaska.Util` (Branch 1)

자주 쓰이는 AnimationCurve를 static 프로퍼티로 제공.

```csharp
public static class EasingCurves
{
    public static AnimationCurve Linear { get; }
    public static AnimationCurve EaseOutQuad { get; }   // 카드 이동 기본
    public static AnimationCurve EaseInQuad { get; }
    public static AnimationCurve EaseInOutQuad { get; }
    public static AnimationCurve EaseOutBack { get; }   // 카드 복귀 시 살짝 튕김
    public static AnimationCurve EaseOutBounce { get; } // 보석 도착 시
}
```

- 정적 캐싱 — 매번 새로 생성하지 않음
- 곡선들은 Inspector에서 시각화하기 위해 ScriptableObject로 빼는 대안도 고려 가능. 일단 코드 정의

---

#### `SubmissionZoneView.cs` 수정 — namespace `Guskapaska.UI` (Branch 1, Branch 2)

Stage 4의 PlayerSlot 빈 상태 시각 문제 해결 + 카드 제출 시 부드러운 슬라이드 지원.

**Branch 1에서:**
- `Clear()` 호출 시 PlayerSlot의 Background Image 알파를 0으로 설정 → 시각적으로 빈 슬롯이 안 보임
- `ShowPlayerCard()` 호출 시 알파를 1로 복원 (단, Stage 4에서 이미 `CardView.Bind()` 안에서 background.color를 UIColors의 색으로 덮어쓰므로 자동 복원될 가능성 있음 — 확인 필요)

**Branch 2에서:**
- 새 메서드 `AnimatePlayerCard(Card, RectTransform sourceTransform)` 추가
- 카드를 PlayerSlot 위치까지 슬라이드 + 스케일 다운 (Stage 4의 즉시 이동을 대체)

```csharp
/// <summary>Animate a player card from its current position to the player slot, then bind.</summary>
public IEnumerator AnimatePlayerCardSubmission(Card card, RectTransform sourceTransform);
```

> 단, `OnPlayerCardDropped` → `gameManager.OnPlayerSubmit(card)` → `OnPlayerCardSubmitted` 이벤트 흐름을 어떻게 조율할지 결정 필요. **권장 접근**: GameUIController가 이벤트를 받아 코루틴으로 애니메이션 시작, 애니메이션이 끝난 후 라운드 진행 흐름이 계속되도록 GameManager 측에서 약간의 지연 허용. 또는 애니메이션은 시각 전용이고 게임 로직은 즉시 진행 (애니메이션이 늦게 따라가는 방식). 후자가 단순하므로 우선 채택.

---

#### `CardInteractable.cs` 수정 — namespace `Guskapaska.UI` (Branch 2)

Stage 4의 즉각 변경을 트윈으로 교체.

- **OnPointerEnter / OnPointerExit**: 호버 시 위치 변경을 0.15초 트윈으로 (현재 즉시)
- **ReturnToOrigin()**: 코루틴 버전 추가. 위치/회전/스케일/부모 복귀를 0.25초 트윈
  - 기존 즉시 복귀 메서드는 `ReturnToOriginInstant()`로 이름 변경 (라운드 종료 시 강제 복귀용)
- **OnBeginDrag**: scale 변경을 0.1초 트윈 (현재 즉시)
- **OnEndDrag**: blocksRaycasts 복원은 그대로 즉시. 복귀 트윈만 부드럽게

```csharp
public void ReturnToOrigin();          // 트윈 버전 (기본)
public void ReturnToOriginInstant();   // 즉시 버전 (긴급 정리용)
```

---

#### `HandView.cs` 수정 — namespace `Guskapaska.UI` (Branch 3)

부채꼴 레이아웃과 딜 애니메이션 추가.

- `arcAngleDegrees`, `arcHeight`를 **0이 아닌 기본값으로** 설정 (예: 15도, 30px)
- 새 메서드 `RenderWithDealAnimation(IReadOnlyList<Card>)`:
  - 카드들을 화면 밖 시작 위치에서 손패 위치로 순차적으로 트윈 (각 카드 사이 0.08초 간격)
  - 매치 시작 시 호출 (`OnMatchStarted`에서 기존 `Render` 대신 사용)

```csharp
public IEnumerator RenderWithDealAnimation(IReadOnlyList<Card> cards);
```

---

#### `AiSubmitAnimator.cs` (신규) — namespace `Guskapaska.UI` (Branch 3)

AI 카드가 AiHandView의 뒷면 카드 더미에서 SubmissionZone/AiSlot까지 포물선 비행.

```csharp
public class AiSubmitAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform aiHandAnchor;     // 비행 시작 지점 (AiHandView의 중앙)
    [SerializeField] private RectTransform aiSlotAnchor;     // 도착 지점 (AiSlot)
    [SerializeField] private GameObject flyingCardPrefab;    // 비행 중에만 표시되는 임시 카드 (CardView 프리팹)
    [SerializeField] private float arcHeight = 100f;
    [SerializeField] private float duration = 0.6f;

    /// <summary>Spawn a temporary flying card and animate it from AI hand to AI slot.</summary>
    public IEnumerator AnimateAiSubmit(Card card);
}
```

- 별도의 임시 CardView 인스턴스를 생성 → 비행 → AiSlot에 도착 시 임시 인스턴스 파괴 후 SubmissionZoneView가 실제 카드 표시
- `MoveLocalArc` 사용

---

#### `GemFlightAnimator.cs` (신규) — namespace `Guskapaska.UI` (Branch 4)

보석 비행 애니메이션. 빈 셀에서 보석이 솟구치고 → 마리오 손이 내려와 보석을 가린 후 → 손이 들어올려지면 → 보석이 플레이어/AI 보석 더미로 비행.

```csharp
public class GemFlightAnimator : MonoBehaviour
{
    [SerializeField] private GameObject gemPrefab;           // 비행 보석
    [SerializeField] private GameObject marioHandPrefab;     // 손 그래픽
    [SerializeField] private RectTransform playerGemTarget;  // 도착 지점 (PlayerGemPile)
    [SerializeField] private RectTransform aiGemTarget;
    [SerializeField] private RectTransform coinGridContainer; // 출발 영역 (CoinGridView의 셀들)

    /// <summary>Animate `count` gems flying from the center grid to the winner's pile.</summary>
    /// <param name="winnerIsPlayer">true → player target, false → AI target.</param>
    public IEnumerator AnimateGemAcquisition(int count, bool winnerIsPlayer);
}
```

연출 시퀀스:
1. CoinGridView의 가장 오른쪽 채워진 셀들 `count`개에서 보석 인스턴스 spawn (현재 위치에서 위로 살짝 솟구침, 0.2초)
2. 마리오 손 그래픽이 위에서 내려옴 (0.3초), 보석들을 가림
3. 손이 잠시 멈춤 (0.15초)
4. 손이 다시 위로 올라가며 사라짐 (0.3초), 동시에 보석들도 시각적으로 사라짐
5. 별도의 보석들이 winner의 GemPile 방향으로 포물선 비행 (0.5초)
6. 도착 시 `GemPileView.SetCount()` 갱신 (기존 즉시 갱신을 이 시점으로 지연)

---

#### `CountdownOverlay.cs` (신규) — namespace `Guskapaska.UI` (Branch 5)

화면 중앙에 큰 3, 2, 1 숫자를 표시하는 오버레이.

```csharp
public class CountdownOverlay : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private AnimationCurve fadeCurve;
    [SerializeField] private float showDuration = 0.7f;

    /// <summary>Show a single number with a fade+scale animation.</summary>
    public IEnumerator ShowNumber(int number);

    /// <summary>Force-hide immediately (e.g. round ended early).</summary>
    public void HideInstant();
}
```

- 숫자가 작게 시작 → 빠르게 확대 (1.5×) → 페이드아웃
- TimerView가 3초 이하로 진입할 때, GameUIController가 매 초 `CountdownOverlay.ShowNumber(...)` 호출
- 동시에 TimerView는 그대로 동작 (오버레이는 추가 시각 효과)

---

#### `GameUIController.cs` 수정 — namespace `Guskapaska.UI` (모든 브랜치 통합)

각 브랜치의 신규 컴포넌트들과 연동:

- **Branch 1**: 변경 없음 (SubmissionZoneView 내부 변경만)
- **Branch 2**: `OnPlayerCardDropped`에서 SubmissionZoneView의 슬라이드 애니메이션 트리거
- **Branch 3**: `OnMatchStarted`에서 `playerHandView.RenderWithDealAnimation` 사용, `OnAiCardSubmitted`에서 `AiSubmitAnimator` 시작
- **Branch 4**: `OnRoundResolved`에서 `GemFlightAnimator.AnimateGemAcquisition` 호출. 단, `OnGemsChanged` 핸들러는 보석 비행이 끝난 시점에만 GemPileView를 갱신하도록 조정
- **Branch 5**: `OnTimerTick` 안에서 3초 이하 진입 감지 → `CountdownOverlay.ShowNumber` 호출

> 이벤트 핸들러 안에서 `StartCoroutine`으로 애니메이션 실행. 게임 로직(라운드 결과 처리)은 애니메이션과 무관하게 즉시 진행되고, 시각만 따라가는 구조.

---

### 2. Prefab 신규/수정

**Branch 1:**
- 신규 프리팹 없음

**Branch 3:**
- `Assets/Prefabs/UI/FlyingCard.prefab` (신규) — AiSubmitAnimator 용 임시 비행 카드. CardView 프리팹을 단순화한 버전 (CardInteractable, DropZone, CanvasGroup 같은 입력 관련 컴포넌트 제거)

**Branch 4:**
- `Assets/Prefabs/UI/FlyingGem.prefab` (신규) — 비행 보석 (작은 시안 사각형 + Image)
- `Assets/Prefabs/UI/MarioHand.prefab` (신규) — 갈색 손 그래픽 (placeholder는 단순 갈색 사각형 + 손가락 형태의 흰색 영역)

> Stage 6에서 placeholder 그래픽이 실제 아트로 교체됨.

---

### 3. Scene 변경 — `Game.unity`

**Branch 3:**
- AiSubmitAnimator GameObject 생성 + 컴포넌트 부착
- 필드 연결: aiHandAnchor, aiSlotAnchor, flyingCardPrefab
- GameUIController에 AiSubmitAnimator 참조 필드 추가

**Branch 4:**
- GemFlightAnimator GameObject 생성 + 컴포넌트 부착
- 필드 연결: gemPrefab, marioHandPrefab, playerGemTarget, aiGemTarget, coinGridContainer
- GameUIController에 GemFlightAnimator 참조 필드 추가

**Branch 5:**
- CountdownOverlay GameObject 생성 (Canvas 최상단)
- 큰 TMP 텍스트 + CanvasGroup
- GameUIController에 CountdownOverlay 참조 필드 추가

---

### 4. Folder Structure After Stage 5

```
Assets/
├── Prefabs/
│   ├── UI/
│   │   ├── CardView.prefab
│   │   ├── CoinCell.prefab
│   │   ├── FlyingCard.prefab        (신규, Branch 3)
│   │   ├── FlyingGem.prefab         (신규, Branch 4)
│   │   ├── MarioHand.prefab         (신규, Branch 4)
│   │   ├── Cards/
│   │   └── Effects/
├── Scripts/
│   ├── Core/                         (변경 없음)
│   ├── Game/                         (변경 없음)
│   ├── UI/
│   │   ├── AiSubmitAnimator.cs       (신규, Branch 3)
│   │   ├── CardInteractable.cs       (수정, Branch 2)
│   │   ├── CardView.cs               (변경 없음)
│   │   ├── CoinGridView.cs           (변경 없음)
│   │   ├── CountdownOverlay.cs       (신규, Branch 5)
│   │   ├── DragController.cs         (변경 없음)
│   │   ├── DrawAccumulatorView.cs    (변경 없음)
│   │   ├── DropZone.cs               (변경 없음)
│   │   ├── GameUIController.cs       (수정, 모든 브랜치)
│   │   ├── GemFlightAnimator.cs      (신규, Branch 4)
│   │   ├── GemPileView.cs            (변경 없음)
│   │   ├── HandView.cs               (수정, Branch 3)
│   │   ├── MenuButton.cs
│   │   ├── ResultPanelController.cs  (변경 없음)
│   │   ├── SubmissionZoneView.cs     (수정, Branch 1, 2)
│   │   ├── TimerView.cs              (변경 없음)
│   │   └── UIColors.cs               (변경 없음)
│   └── Util/
│       ├── EasingCurves.cs           (신규, Branch 1)
│       └── TweenRunner.cs            (신규, Branch 1)
├── Scenes/
│   └── Game.unity                    (애니메이터 GameObject들 추가)
└── Tests/                            (변경 없음)
```

---

## Animation Spec (지속 시간 + 이징)

각 애니메이션의 권장 시간과 이징. 구현 시 이 값을 SerializeField로 노출하여 튜닝 가능하게 할 것.

| 애니메이션 | 지속 (초) | 이징 | 비고 |
|---|---|---|---|
| 카드 호버 진입/이탈 | 0.15 | EaseOutQuad | 짧고 즉각적 |
| 드래그 시작 (scale 1→1.2) | 0.10 | EaseOutQuad | 매우 짧음 |
| 드롭 실패 복귀 | 0.25 | EaseOutBack | 살짝 튕김 |
| 플레이어 카드 제출 (슬라이드 + 스케일) | 0.30 | EaseOutQuad | |
| AI 카드 포물선 비행 | 0.60 | EaseInOutQuad | arcHeight=100 |
| 카드 딜 (매치 시작) | 0.40 / 카드 | EaseOutQuad | 카드 간 0.08초 간격 |
| 보석 솟구침 | 0.20 | EaseOutQuad | |
| 마리오 손 내려옴 | 0.30 | EaseOutQuad | |
| 마리오 손 멈춤 | 0.15 | — | 정지 시간 |
| 마리오 손 올라감 | 0.30 | EaseInQuad | |
| 보석 비행 (포물선) | 0.50 | EaseInOutQuad | arcHeight=80 |
| 카운트다운 숫자 (확대 + 페이드) | 0.70 | EaseOutQuad → Linear (페이드) | 매 초 |

---

## Output Format (Claude에게 요구하는 출력 형식)

각 브랜치 작업 시 다음을 제출:

1. **File tree**: 신규/수정 파일 전체 경로
2. **Source code**: 각 `.cs` 파일의 완전한 내용. 메서드 본문 안의 주석은 한국어, 메서드 위 XML 문서 주석은 영어
3. **Prefab 신규/수정 체크리스트**: 컴포넌트 추가 + 필드 연결 단계
4. **Scene 수정 체크리스트**: 신규 GameObject + 필드 연결 단계
5. **Animation tuning notes**: 어떤 값들이 Serialized로 노출되는지, Inspector에서 조정 가능한 항목 목록
6. **Verification steps**: Play 모드에서 확인할 항목 + 기존 회귀 테스트 통과

---

## Constraints

- **외부 트윈 라이브러리 사용 금지** — Coroutine + AnimationCurve만 사용
- **모든 트윈 지속시간은 SerializeField** — Inspector에서 튜닝 가능
- **애니메이션 중 사용자 입력 차단 가능**: 단, 차단할지 통과시킬지는 케이스별로 결정
  - 카드 딜 중 → 입력 차단 (CardInteractable.Interactable=false)
  - AI 카드 비행 중 → 입력 허용 (플레이어는 이미 제출한 상태)
  - 보석 비행 중 → 입력 차단 (라운드 종료 → 다음 라운드 시작까지)
- **시각과 게임 로직의 분리**: GameState 갱신은 즉시, 시각은 애니메이션으로 따라감. 게임 로직이 애니메이션 완료를 기다리지 않음
- **Coroutine만 사용**: async/await 금지 (`02_Unity6_Guidelines.md §11`)
- **`FindObjectOfType` 등 deprecated API 금지**
- **모든 신규 컴포넌트는 OnDisable에서 진행 중인 트윈을 취소** (`TweenRunner.CancelAll(this)`)
- **Stage 4의 미해결 시각 이슈(PlayerSlot 빈 상태)는 Branch 1에서 함께 처리**
- **AI 비행 카드의 face up/down**: Stage 6에서 결정 (현재는 face-down으로 비행하다가 도착 후 face-up으로 표시)

---

## Known Pitfalls

- **Coroutine 누수**: 게임 오브젝트가 비활성화될 때 진행 중인 트윈을 명시적으로 취소하지 않으면 다음 활성화 시 이중 실행 위험. `TweenRunner` 키 기반 취소 활용
- **부동소수점 누적 오차**: `Lerp` 반복 후 최종 값이 정확히 to와 같지 않을 수 있음. 트윈 마지막에 명시적으로 to 값 대입
- **순환 트윈**: 호버 진입 트윈이 끝나기 전에 드래그 시작 트윈이 호출되면 위치가 꼬임. `TweenRunner.Cancel`로 이전 트윈을 명시적 정리
- **AI 카드 비행과 SubmissionZoneView 동기화**: 비행 카드가 도착하는 시점과 SubmissionZoneView.ShowAiCard 호출 시점이 정확히 맞아야 함. AiSubmitAnimator 코루틴 끝부분에서 SubmissionZoneView 호출
- **보석 비행과 GemPileView 카운트 동기화**: 게임 로직상 GemsChanged 이벤트는 즉시 발화. 시각상 보석 비행이 끝난 후에 GemPileView가 갱신되어야 자연스러움. GameUIController가 OnGemsChanged 직접 처리하지 않고, GemFlightAnimator가 도착 시 GemPileView 갱신을 트리거하는 구조 권장
- **카운트다운 오버레이와 타이머**: TimerView의 SetUrgent(true)와 동시에 동작. 두 효과가 겹쳐도 자연스러워야 함. 카운트다운 오버레이는 화면 중앙, TimerView는 우측 상단 → 위치 분리됨
- **부채꼴 손패의 클릭 영역**: 카드가 회전되면 Image의 raycast 영역도 회전됨. CardInteractable의 입력은 그대로 동작하지만, Stage 4의 §16 (Raycast Target) 점검을 다시 확인

---

## Stage 5 완료 후 Stage 6 미리보기

Stage 6은 폴리시 단계:
- 사운드 효과 통합 (`AudioManager` 와이어업)
- 실제 카드 아트 슬롯 도입 (placeholder 색상/텍스트 → 일러스트)
- 결과 화면 시각 다듬기
- 메인 메뉴 진입/이탈 트랜지션

따라서 Stage 5에서는 **mechanic 자체에만 집중**하고, 시각 디테일은 Stage 6에서 마무리.
