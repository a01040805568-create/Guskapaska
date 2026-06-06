using System;
using System.Collections.Generic;
using UnityEngine;
using Guskapaska.Core;
using Guskapaska.Game;
using Guskapaska.UI;
using Guskapaska.Util;

namespace Guskapaska.Tutorial
{
    /// <summary>
    /// 튜토리얼 전체 흐름을 제어한다. 단계 시퀀스를 순회하며 오버레이를 갱신하고,
    /// 단계별로 플레이어 입력을 게이팅하며, 게임 이벤트로 단계 진행을 트리거한다.
    /// 기존 게임 시스템에 비침투적 — GameEvents 구독과 DragController 게이팅만 사용한다.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        [Header("References")]
        // 게임 이벤트로 단계 진행을 트리거하기 위한 참조. 없으면 NextButton 트리거만 동작한다.
        [SerializeField] private GameManager gameManager;

        // 단계별 카드 드래그 허용/차단을 위한 참조. 없으면 입력 게이팅을 건너뛴다.
        [SerializeField] private DragController dragController;

        // 오버레이 표시 담당 뷰.
        [SerializeField] private TutorialOverlayView overlay;

        [Header("Steps")]
        // 튜토리얼 단계 시퀀스. Branch 1에서는 비어 있어도 안전하게 동작한다 (Branch 2에서 채움).
        [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

        [Header("Options")]
        // 활성화되면 Start에서 자동으로 튜토리얼을 시작한다 (단독 테스트용).
        [SerializeField] private bool beginOnStart = false;

        /// <summary>튜토리얼 완료 또는 건너뛰기 시 발생. 인자는 끝까지 완료했는지 여부.</summary>
        public event Action<bool> OnTutorialFinished;

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
            // 진행 중이던 튜토리얼을 안전하게 정리 (구독 해제 + 코루틴 취소).
            UnsubscribeEvents();
            UnhookOverlay();
            TweenRunner.CancelAll(this);
        }

        // ─────────────────────────────────────────────────────────────
        // 공개 API
        // ─────────────────────────────────────────────────────────────

        /// <summary>튜토리얼을 첫 단계부터 시작한다.</summary>
        public void Begin()
        {
            if (_running) return;
            if (overlay == null)
            {
                Debug.LogError("[TutorialController] overlay 가 연결되지 않았습니다.");
                return;
            }

            _running = true;
            _currentIndex = -1;

            SubscribeEvents();
            HookOverlay();

            overlay.FadeIn();

            // 첫 단계로 진입.
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

            // 입력 게이팅 복원 — 종료 후 일반 게임에서 드래그가 막히지 않도록.
            if (dragController != null)
            {
                dragController.SetAllInteractable(true);
            }

            OnTutorialFinished?.Invoke(completed);
        }

        // ─────────────────────────────────────────────────────────────
        // 단계 전환
        // ─────────────────────────────────────────────────────────────

        // 지정 인덱스의 단계로 이동한다. 범위를 벗어나면 완료 처리.
        private void GoToStep(int index)
        {
            if (index < 0 || index >= steps.Count)
            {
                // 더 표시할 단계가 없으면 완료.
                EndTutorial(completed: true);
                return;
            }

            _currentIndex = index;
            TutorialStep step = steps[index];

            // "다음 버튼" 트리거인 단계만 다음 버튼을 노출한다.
            bool showNext = step.advanceTrigger == TutorialAdvanceTrigger.NextButton;
            overlay.ShowStep(step.bodyText, step.highlightTarget, showNext);

            // 단계별 드래그 허용/차단.
            if (dragController != null)
            {
                dragController.SetAllInteractable(step.allowPlayerDrag);
            }
        }

        // 현재 단계의 트리거가 주어진 종류와 일치하면 다음 단계로 진행.
        private void TryAdvanceOn(TutorialAdvanceTrigger trigger)
        {
            if (!_running) return;
            if (_currentIndex < 0 || _currentIndex >= steps.Count) return;

            if (steps[_currentIndex].advanceTrigger == trigger)
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
            events.OnPlayerCardSubmitted += HandlePlayerCardSubmitted;
            events.OnRoundResolved += HandleRoundResolved;
            events.OnMatchEnded += HandleMatchEnded;

            _subscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_subscribed || gameManager == null || gameManager.Events == null) return;

            GameEvents events = gameManager.Events;
            events.OnPlayerCardSubmitted -= HandlePlayerCardSubmitted;
            events.OnRoundResolved -= HandleRoundResolved;
            events.OnMatchEnded -= HandleMatchEnded;

            _subscribed = false;
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