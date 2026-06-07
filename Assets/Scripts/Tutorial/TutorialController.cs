using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Guskapaska.Core;
using Guskapaska.Game;
using Guskapaska.UI;
using Guskapaska.Util;

namespace Guskapaska.Tutorial
{
    /// <summary>
    /// 튜토리얼 전체 흐름을 제어한다. 고정 손패와 스크립트 AI로 튜토리얼 매치를 시작하고,
    /// 단계 시퀀스를 순회하며 오버레이를 갱신하고, 단계별로 입력을 게이팅하며,
    /// 게임 이벤트로 단계 진행을 트리거한다.
    /// 게임 로직 본문은 수정하지 않는다 — 공개 API(StartTutorialMatch, SetInteractableByShape,
    /// StopTimer) 호출과 GameEvents 구독만으로 동작한다.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        [Header("References")]
        // 튜토리얼 매치 시작·이벤트 트리거에 사용.
        [SerializeField] private GameManager gameManager;

        // 단계별 드래그 게이팅에 사용.
        [SerializeField] private DragController dragController;

        // 안내를 읽는 동안 라운드 타이머를 멈추기 위해 사용.
        [SerializeField] private TimerController timerController;

        // 오버레이 표시 담당 뷰.
        [SerializeField] private TutorialOverlayView overlay;

        [Header("Highlight Targets")]
        // 시나리오에서 강조할 씬 오브젝트들. BuildScenario에서 단계에 연결한다.
        [SerializeField] private RectTransform playerHandHighlight;
        [SerializeField] private RectTransform gemPileHighlight;
        [SerializeField] private RectTransform drawAccumulatorHighlight;

        [Header("Options")]
        // 활성화되면 Start에서 자동으로 튜토리얼을 시작한다 (단독 테스트용).
        [SerializeField] private bool beginOnStart = false;

        /// <summary>튜토리얼 완료 또는 건너뛰기 시 발생. 인자는 끝까지 완료했는지 여부.</summary>
        public event Action<bool> OnTutorialFinished;

        // 코드로 생성되는 단계 시퀀스.
        private List<TutorialStep> _steps = new List<TutorialStep>();

        private int _currentIndex = -1;
        private bool _running;
        private bool _subscribed;
        private bool _overlayHooked;

        private void Start()
        {
            if (beginOnStart)
            {
                Begin();
            }
        }

        private void OnDisable()
        {
            // 진행 중이던 튜토리얼을 안전하게 정리.
            UnsubscribeEvents();
            UnhookOverlay();
            StopAllCoroutines();
            TweenRunner.CancelAll(this);
        }

        // ─────────────────────────────────────────────────────────────
        // 공개 API
        // ─────────────────────────────────────────────────────────────

        /// <summary>고정 손패와 스크립트 AI로 튜토리얼 매치를 시작하고 첫 단계를 표시한다.</summary>
        public void Begin()
        {
            if (_running) return;
            if (overlay == null || gameManager == null)
            {
                Debug.LogError("[TutorialController] overlay 또는 gameManager 가 연결되지 않았습니다.");
                return;
            }

            _running = true;
            _currentIndex = -1;

            // 시나리오 단계 구성.
            _steps = BuildScenario();

            SubscribeEvents();      // ← 매치 시작 "앞"으로 이동
            HookOverlay();

            IAiStrategy scripted = new ScriptedAiStrategy(
                new List<CardShape> { CardShape.Scissors, CardShape.Paper, CardShape.Scissors },
                new AiRandomStrategy());
            gameManager.StartTutorialMatch(BuildPlayerHand(), BuildAiHand(), scripted);

            overlay.FadeIn();

            GoToStep(0);
        }

        /// <summary>다음 단계로 진행한다. 마지막 단계였다면 튜토리얼을 완료한다.</summary>
        public void Advance()
        {
            if (!_running) return;
            GoToStep(_currentIndex + 1);
        }

        /// <summary>튜토리얼을 종료하고 정리한다.</summary>
        /// <param name="completed">끝까지 완료했으면 true, 건너뛰기면 false.</param>
        public void EndTutorial(bool completed)
        {
            if (!_running) return;
            _running = false;

            UnsubscribeEvents();
            UnhookOverlay();

            if (overlay != null)
            {
                overlay.FadeOut();
            }

            // 입력 게이팅 복원 — 종료 후 자유 플레이에서 드래그가 막히지 않도록.
            if (dragController != null)
            {
                dragController.SetAllInteractable(true);
            }

            OnTutorialFinished?.Invoke(completed);
        }

        // ─────────────────────────────────────────────────────────────
        // 시나리오 구성
        // ─────────────────────────────────────────────────────────────

        // 12단계 시나리오를 코드로 생성한다. 안내 문구는 자유롭게 조정 가능.
        private List<TutorialStep> BuildScenario()
        {
            return new List<TutorialStep>
            {
                Step("구스카파스카에 오신 걸 환영합니다!\n가위바위보로 보석을 모으는 게임이에요.",
                    null, TutorialAdvanceTrigger.NextButton, DragGate.Block),

                Step("이건 당신의 카드예요.\n각 카드는 가위·바위·보 모양과 코인 숫자를 가져요.",
                    playerHandHighlight, TutorialAdvanceTrigger.NextButton, DragGate.Block),

                Step("가위는 보를, 보는 바위를, 바위는 가위를 이겨요.",
                    null, TutorialAdvanceTrigger.NextButton, DragGate.Block),

                Step("가운데 보석을 두고 겨뤄요.\n라운드에서 이기면 두 카드의 코인 합만큼 보석을 가져와요.",
                    gemPileHighlight, TutorialAdvanceTrigger.NextButton, DragGate.Block),

                Step("비기면 두 카드의 코인이 가운데 쌓이고,\n다음에 이기는 사람이 한꺼번에 가져가요.",
                    null, TutorialAdvanceTrigger.NextButton, DragGate.Block),

                Step("먼저 비기는 걸 해볼게요.\n가위 카드를 슬롯으로 드래그해 내보세요.",
                    playerHandHighlight, TutorialAdvanceTrigger.RoundResolved, DragGate.AllowShape, CardShape.Scissors),

                Step("비겼어요!\n두 카드의 코인이 가운데에 쌓였어요.",
                    drawAccumulatorHighlight, TutorialAdvanceTrigger.NextButton, DragGate.Block),

                Step("한 번 더 비겨볼게요.\n이번엔 보 카드를 내보세요.",
                    playerHandHighlight, TutorialAdvanceTrigger.RoundResolved, DragGate.AllowShape, CardShape.Paper),

                Step("또 비겨서 쌓인 코인이 더 많아졌어요!",
                    drawAccumulatorHighlight, TutorialAdvanceTrigger.NextButton, DragGate.Block),

                Step("이제 이겨봐요!\n바위 카드를 내서 AI의 가위를 이기세요.",
                    playerHandHighlight, TutorialAdvanceTrigger.RoundResolved, DragGate.AllowShape, CardShape.Rock),

                Step("이겼어요!\n쌓여 있던 코인까지 한꺼번에 보석으로 가져왔어요.",
                    gemPileHighlight, TutorialAdvanceTrigger.NextButton, DragGate.Block),

                Step("이제 규칙을 다 익혔어요!\n남은 라운드를 직접 플레이해서 보석을 모아보세요.",
                    null, TutorialAdvanceTrigger.NextButton, DragGate.Block),
            };
        }

        // TutorialStep 생성 헬퍼.
        private static TutorialStep Step(
            string body,
            RectTransform highlight,
            TutorialAdvanceTrigger trigger,
            DragGate gate,
            CardShape shape = CardShape.Scissors)
        {
            return new TutorialStep
            {
                bodyText = body,
                highlightTarget = highlight,
                advanceTrigger = trigger,
                dragGate = gate,
                allowedShape = shape
            };
        }

        // 플레이어 고정 손패. 시연용 가위·보·바위 + 자유 플레이용 여분 2장.
        private static List<Card> BuildPlayerHand()
        {
            return new List<Card>
            {
                new Card("Tut_P_Sci", CardShape.Scissors, 1),
                new Card("Tut_P_Pap", CardShape.Paper, 1),
                new Card("Tut_P_Roc", CardShape.Rock, 2),
                new Card("Tut_P_Sci2", CardShape.Scissors, 1),
                new Card("Tut_P_Pap2", CardShape.Paper, 1),
            };
        }

        // AI 고정 손패. 스크립트 시퀀스(가위 → 보 → 가위)가 찾을 카드 + 여분.
        private static List<Card> BuildAiHand()
        {
            return new List<Card>
            {
                new Card("Tut_A_Sci", CardShape.Scissors, 1),
                new Card("Tut_A_Pap", CardShape.Paper, 1),
                new Card("Tut_A_Sci2", CardShape.Scissors, 1),
                new Card("Tut_A_Roc", CardShape.Rock, 1),
                new Card("Tut_A_Pap2", CardShape.Paper, 1),
            };
        }

        // ─────────────────────────────────────────────────────────────
        // 단계 전환
        // ─────────────────────────────────────────────────────────────

        // 지정 인덱스의 단계로 이동한다. 범위를 벗어나면 완료 처리.
        private void GoToStep(int index)
        {
            if (index < 0 || index >= _steps.Count)
            {
                EndTutorial(completed: true);
                return;
            }

            _currentIndex = index;
            TutorialStep step = _steps[index];

            // "다음 버튼" 트리거인 단계만 다음 버튼을 노출한다.
            bool showNext = step.advanceTrigger == TutorialAdvanceTrigger.NextButton;
            overlay.ShowStep(step.bodyText, step.highlightTarget, showNext);

            // 단계별 입력 게이팅.
            ApplyGate(step);
        }

        // 단계의 게이트 모드에 따라 드래그 허용 범위를 설정한다.
        private void ApplyGate(TutorialStep step)
        {
            if (dragController == null) return;

            switch (step.dragGate)
            {
                case DragGate.Block:
                    dragController.SetAllInteractable(false);
                    break;
                case DragGate.AllowAll:
                    dragController.SetAllInteractable(true);
                    break;
                case DragGate.AllowShape:
                    dragController.SetInteractableByShape(step.allowedShape);
                    break;
            }
        }

        // 현재 단계의 트리거가 주어진 종류와 일치하면 다음 단계로 진행.
        private void TryAdvanceOn(TutorialAdvanceTrigger trigger)
        {
            if (!_running) return;
            if (_currentIndex < 0 || _currentIndex >= _steps.Count) return;

            if (_steps[_currentIndex].advanceTrigger == trigger)
            {
                Advance();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 오버레이 버튼 연결 / 해제
        // ─────────────────────────────────────────────────────────────

        private void HookOverlay()
        {
            if (_overlayHooked || overlay == null) return;
            overlay.OnNextClicked += HandleNextClicked;
            overlay.OnSkipClicked += HandleSkipClicked;
            _overlayHooked = true;
        }

        private void UnhookOverlay()
        {
            if (!_overlayHooked || overlay == null) return;
            overlay.OnNextClicked -= HandleNextClicked;
            overlay.OnSkipClicked -= HandleSkipClicked;
            _overlayHooked = false;
        }

        private void HandleNextClicked()
        {
            TryAdvanceOn(TutorialAdvanceTrigger.NextButton);
        }

        private void HandleSkipClicked()
        {
            // 건너뛰기는 어느 단계에서든 즉시 종료.
            EndTutorial(completed: false);
        }

        // ─────────────────────────────────────────────────────────────
        // 게임 이벤트 구독 / 해제
        // ─────────────────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (_subscribed || gameManager == null || gameManager.Events == null) return;

            GameEvents events = gameManager.Events;
            events.OnRoundStarted += HandleRoundStarted;
            events.OnPlayerCardSubmitted += HandlePlayerCardSubmitted;
            events.OnRoundResolved += HandleRoundResolved;
            events.OnMatchEnded += HandleMatchEnded;

            _subscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_subscribed || gameManager == null || gameManager.Events == null) return;

            GameEvents events = gameManager.Events;
            events.OnRoundStarted -= HandleRoundStarted;
            events.OnPlayerCardSubmitted -= HandlePlayerCardSubmitted;
            events.OnRoundResolved -= HandleRoundResolved;
            events.OnMatchEnded -= HandleMatchEnded;

            _subscribed = false;
        }

        private void HandleRoundStarted(int roundNumber)
        {
            // RoundController가 OnRoundStarted 직후 타이머를 켜므로,
            // 한 프레임 뒤에 멈춰 안내를 읽는 동안 시간이 흐르지 않게 한다.
            if (_running)
            {
                StartCoroutine(StopTimerNextFrame());
            }
        }

        // 타이머가 켜진 다음 프레임에 정지시킨다.
        private IEnumerator StopTimerNextFrame()
        {
            yield return null;
            if (_running && timerController != null)
            {
                timerController.StopTimer();
            }
        }

        private void HandlePlayerCardSubmitted(Card card)
        {
            TryAdvanceOn(TutorialAdvanceTrigger.PlayerCardSubmitted);
        }

        private void HandleRoundResolved(RoundOutcome outcome)
        {
            TryAdvanceOn(TutorialAdvanceTrigger.RoundResolved);
        }

        private void HandleMatchEnded(MatchResult result)
        {
            TryAdvanceOn(TutorialAdvanceTrigger.MatchEnded);
        }
    }
}