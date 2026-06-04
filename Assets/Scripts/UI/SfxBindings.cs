using UnityEngine;
using Guskapaska.Audio;
using Guskapaska.Core;
using Guskapaska.Game;

namespace Guskapaska.UI
{
    /// <summary>
    /// Routes gameplay events to one-shot SFX playback. This is a sibling
    /// responsibility to <see cref="GameUIController"/>: the UI controller owns
    /// visuals, while SfxBindings owns sound. It subscribes to <see cref="GameEvents"/>
    /// (via <see cref="GameManager"/>) and to the <see cref="DragController"/>'s
    /// aggregate card-input events, then plays clips through <see cref="AudioManager"/>.
    /// </summary>
    /// <remarks>
    /// All playback is null-safe: a missing clip or a missing AudioManager is a no-op,
    /// so the game stays fully playable before any SFX assets are imported.
    /// </remarks>
    public class SfxBindings : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private DragController dragController;

        [Header("Card SFX")]
        [SerializeField] private AudioClip cardHoverSfx;
        [SerializeField] private AudioClip cardDragStartSfx;
        [SerializeField] private AudioClip cardSubmitSfx;
        [SerializeField] private AudioClip cardReturnSfx;

        [Header("Round SFX")]
        [SerializeField] private AudioClip roundStartSfx;
        [SerializeField] private AudioClip roundWinSfx;
        [SerializeField] private AudioClip roundLoseSfx;
        [SerializeField] private AudioClip roundDrawSfx;

        [Header("Game SFX")]
        [SerializeField] private AudioClip countdownTickSfx;
        [SerializeField] private AudioClip matchWinSfx;
        [SerializeField] private AudioClip matchLoseSfx;
        [SerializeField] private AudioClip gemCollectSfx;

        [Header("Countdown Settings")]
        [Tooltip("이 초 이하에서 카운트다운 틱 SFX가 재생된다. GameUIController의 값과 일치시킬 것.")]
        [SerializeField] private int countdownThreshold = 3;

        private bool _subscribed;

        // 보석 변화량 계산용 — 마지막으로 본 플레이어/AI 보석 수.
        private int _lastPlayerGems;
        private int _lastAiGems;

        // 카운트다운 틱 중복 방지용 — 마지막으로 틱을 울린 정수 초.
        // OnTimerTick은 매 프레임 호출되므로 정수 초가 바뀌는 순간에만 재생해야 한다.
        private int _lastCountdownNumber = int.MaxValue;

        private void Start()
        {
            if (gameManager == null)
            {
                Debug.LogError("[SfxBindings] gameManager 가 연결되지 않았습니다. Inspector에서 GameManager를 연결하세요.");
                return;
            }

            Subscribe();

            // SfxBindings.Start가 GameUIController/GameManager보다 늦게 실행되면 OnMatchStarted를
            // 놓칠 수 있다. 현재 상태로 보석 추적 값을 강제 동기화해 첫 OnGemsChanged에서
            // 잘못된 획득 SFX가 울리는 것을 방지한다 (구독 시점 함정 대비).
            if (gameManager.State != null)
            {
                _lastPlayerGems = gameManager.State.PlayerGems;
                _lastAiGems = gameManager.State.AiGems;
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // ─────────────────────────────────────────────────────────────
        // 구독 / 해제
        // ─────────────────────────────────────────────────────────────

        private void Subscribe()
        {
            if (_subscribed || gameManager == null || gameManager.Events == null) return;

            GameEvents events = gameManager.Events;
            events.OnMatchStarted += HandleMatchStarted;
            events.OnRoundStarted += HandleRoundStarted;
            events.OnRoundResolved += HandleRoundResolved;
            events.OnGemsChanged += HandleGemsChanged;
            events.OnMatchEnded += HandleMatchEnded;
            events.OnTimerTick += HandleTimerTick;

            if (dragController != null)
            {
                dragController.OnPlayerCardSubmitted += HandleCardSubmitted;
                dragController.OnAnyCardDragStarted += HandleCardDragStarted;
                dragController.OnAnyCardReturned += HandleCardReturned;
                dragController.OnAnyCardHovered += HandleCardHovered;
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || gameManager == null || gameManager.Events == null) return;

            GameEvents events = gameManager.Events;
            events.OnMatchStarted -= HandleMatchStarted;
            events.OnRoundStarted -= HandleRoundStarted;
            events.OnRoundResolved -= HandleRoundResolved;
            events.OnGemsChanged -= HandleGemsChanged;
            events.OnMatchEnded -= HandleMatchEnded;
            events.OnTimerTick -= HandleTimerTick;

            if (dragController != null)
            {
                dragController.OnPlayerCardSubmitted -= HandleCardSubmitted;
                dragController.OnAnyCardDragStarted -= HandleCardDragStarted;
                dragController.OnAnyCardReturned -= HandleCardReturned;
                dragController.OnAnyCardHovered -= HandleCardHovered;
            }

            _subscribed = false;
        }

        // ─────────────────────────────────────────────────────────────
        // 카드 입력 SFX (DragController 집계 이벤트 경유)
        // ─────────────────────────────────────────────────────────────

        // 카드 위에 포인터 진입. 호버 SFX는 빈번하므로 클립을 비워 무음 처리해도 된다.
        private void HandleCardHovered() => Play(cardHoverSfx);

        // 플레이어 카드 드래그 시작.
        private void HandleCardDragStarted() => Play(cardDragStartSfx);

        // 플레이어 카드 제출 성공 (DropZone에 정상 드롭).
        private void HandleCardSubmitted(CardInteractable card) => Play(cardSubmitSfx);

        // 드롭 실패로 카드가 원위치로 복귀.
        private void HandleCardReturned() => Play(cardReturnSfx);

        // ─────────────────────────────────────────────────────────────
        // 라운드 / 매치 SFX (GameEvents 경유)
        // ─────────────────────────────────────────────────────────────

        private void HandleMatchStarted(GameState state)
        {
            // 새 매치: 보석/카운트다운 추적 값 초기화.
            _lastPlayerGems = state != null ? state.PlayerGems : 0;
            _lastAiGems = state != null ? state.AiGems : 0;
            _lastCountdownNumber = int.MaxValue;
        }

        private void HandleRoundStarted(int roundNumber)
        {
            // 새 라운드: 카운트다운 추적 초기화 (이전 라운드 1초 → 새 라운드 큰 값으로 점프하며
            // 카운트다운 틱이 다시 트리거되도록).
            _lastCountdownNumber = int.MaxValue;
            Play(roundStartSfx);
        }

        private void HandleRoundResolved(RoundOutcome outcome)
        {
            if (outcome == null) return;

            // 라운드 승자에 따라 win/lose/draw SFX 선택.
            switch (outcome.Winner)
            {
                case RoundWinner.Player: Play(roundWinSfx); break;
                case RoundWinner.Ai:     Play(roundLoseSfx); break;
                case RoundWinner.None:   Play(roundDrawSfx); break;
            }
        }

        private void HandleGemsChanged(int player, int ai, int center)
        {
            // 어느 쪽이든 보석이 증가한 경우(획득 발생)에만 1회 재생.
            int playerDelta = player - _lastPlayerGems;
            int aiDelta = ai - _lastAiGems;

            if (playerDelta > 0 || aiDelta > 0)
            {
                Play(gemCollectSfx);
            }

            _lastPlayerGems = player;
            _lastAiGems = ai;
        }

        private void HandleMatchEnded(MatchResult result)
        {
            if (result == null) return;

            // 매치 결과에 따라 승/패 SFX. 무승부 전용 클립은 없으므로 침묵 처리.
            switch (result.Winner)
            {
                case MatchWinner.Player: Play(matchWinSfx); break;
                case MatchWinner.Ai:     Play(matchLoseSfx); break;
                case MatchWinner.Tie:    break;
            }
        }

        private void HandleTimerTick(float secondsRemaining)
        {
            // GameUIController와 동일한 올림 방식으로 정수 초 산출.
            int displaySeconds = secondsRemaining < 0f ? 0 : Mathf.CeilToInt(secondsRemaining);

            // 임계값 이하 + 0 초과 + 정수 초가 바뀌는 순간에만 1회 재생.
            if (displaySeconds <= countdownThreshold
                && displaySeconds > 0
                && displaySeconds != _lastCountdownNumber)
            {
                _lastCountdownNumber = displaySeconds;
                Play(countdownTickSfx);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 안전 재생 헬퍼
        // ─────────────────────────────────────────────────────────────

        // AudioManager가 없거나 클립이 null이어도 안전하게 동작한다 (무음).
        private void Play(AudioClip clip)
        {
            if (clip == null) return;
            AudioManager.Instance?.PlaySfx(clip);
        }
    }
}
