using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Game;
using Guskapaska.Util;

namespace Guskapaska.UI
{
    /// <summary>
    /// End-of-match overlay. Shown when GameEvents.OnMatchEnded fires.
    /// Provides Restart and Menu buttons.
    /// Stage 6 adds a polished entrance (fade + scale) and a score count-up,
    /// all driven through the shared <see cref="TweenRunner"/>.
    /// </summary>
    public class ResultPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI titleText;       // "승리!" / "패배" / "무승부"
        [SerializeField] private TextMeshProUGUI scoreText;       // "보석: 8 vs 5"
        [SerializeField] private TextMeshProUGUI reasonText;      // "중앙 보석 소진" 등
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        [Header("Animation Settings (Stage 6)")]
        [SerializeField] private float appearDuration = 0.5f;
        [SerializeField] private float scoreCountupDuration = 1.0f;
        [Tooltip("페이드 인 대상. 패널은 활성 상태를 유지하고 알파 0에서 1로 트윈된다.")]
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("스케일 등장 대상. 0.8에서 1.0으로 EaseOutBack 트윈된다.")]
        [SerializeField] private RectTransform panelTransform;

        // 등장 시 시작 스케일 (0.8 → 1.0). 디자인 사양 고정값.
        private const float AppearStartScale = 0.8f;

        // TweenRunner 취소 키 — 호스트(this)별로 유일하면 충분하므로 상수로 둔다.
        private const string FadeKey = "result_fade";
        private const string ScaleKey = "result_scale";
        private const string CountupKey = "result_countup";

        private void Awake()
        {
            // 시작 시 패널 비활성화
            if (panel != null) panel.SetActive(false);

            // 버튼 리스너 등록 (씬 내내 유효)
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenuClicked);
        }

        private void OnDisable()
        {
            // 씬 언로드/비활성화 시 진행 중인 트윈·카운트업 코루틴을 모두 중단해
            // stale 코루틴이 남지 않도록 한다.
            TweenRunner.CancelAll(this);
        }

        /// <summary>
        /// Display the result panel populated from a MatchResult,
        /// playing the entrance animation and counting the score up from zero.
        /// </summary>
        public void Show(MatchResult result)
        {
            if (panel == null || result == null) return;

            // 패널 활성화 (트윈 호스트는 this 컨트롤러이므로, 방금 활성화된 패널이어도 트윈 가능)
            panel.SetActive(true);

            // 승자에 따른 제목 텍스트와 색상 결정
            switch (result.Winner)
            {
                case MatchWinner.Player:
                    if (titleText != null)
                    {
                        titleText.text = "승리!";
                        titleText.color = UIColors.ResultWin;
                    }
                    break;
                case MatchWinner.Ai:
                    if (titleText != null)
                    {
                        titleText.text = "패배";
                        titleText.color = UIColors.ResultLose;
                    }
                    break;
                case MatchWinner.Tie:
                    if (titleText != null)
                    {
                        titleText.text = "무승부";
                        titleText.color = UIColors.ResultTie;
                    }
                    break;
            }

            // 종료 사유를 한글로 변환
            if (reasonText != null)
            {
                reasonText.text = TranslateReason(result.EndReason);
            }

            // 점수 텍스트는 0부터 카운트업하므로, 시작 직전에 0,0으로 세팅해 이전 값이 깜빡이지 않게 함.
            if (scoreText != null)
            {
                scoreText.text = FormatScore(0, 0);
            }

            // ── 등장 애니메이션 시작 ──

            // 초기 상태로 리셋 (재표시 시 이전 값이 남지 않도록).
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (panelTransform != null) panelTransform.localScale = Vector3.one * AppearStartScale;

            // 알파 0 → 1 (EaseOutQuad)
            if (canvasGroup != null)
            {
                TweenRunner.Run(this, FadeKey,
                    TweenRunner.FadeCanvasGroup(canvasGroup, 0f, 1f, appearDuration, EasingCurves.EaseOutQuad));
            }

            // 스케일 0.8 → 1.0 (EaseOutBack — 살짝 오버슈트하며 "톡" 안착)
            if (panelTransform != null)
            {
                TweenRunner.Run(this, ScaleKey,
                    TweenRunner.Scale(panelTransform, Vector3.one * AppearStartScale, Vector3.one, appearDuration, EasingCurves.EaseOutBack));
            }

            // 점수 카운트업 0 → 최종 (Linear)
            TweenRunner.Run(this, CountupKey,
                CountUpScore(result.PlayerGems, result.AiGems, scoreCountupDuration));
        }

        /// <summary>
        /// Hide the result panel immediately, cancelling any in-flight entrance/count-up tweens.
        /// </summary>
        public void Hide()
        {
            // 카운트업 도중 닫히는 경우 stale 코루틴 방지.
            TweenRunner.CancelAll(this);
            if (panel != null) panel.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────
        // 점수 카운트업
        // ─────────────────────────────────────────────────────────────

        // 플레이어/AI 보석 수를 0부터 최종 값까지 선형 보간하며 매 프레임 텍스트를 갱신.
        private IEnumerator CountUpScore(int playerFinal, int aiFinal, float duration)
        {
            if (scoreText == null) yield break;

            // 지속시간이 0 이하면 즉시 최종 값 표기 후 종료.
            if (duration <= 0f)
            {
                scoreText.text = FormatScore(playerFinal, aiFinal);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration); // Linear

                int p = Mathf.RoundToInt(Mathf.Lerp(0f, playerFinal, u));
                int a = Mathf.RoundToInt(Mathf.Lerp(0f, aiFinal, u));
                scoreText.text = FormatScore(p, a);

                yield return null;
            }

            // 누적 오차 방지를 위해 마지막에 정확한 최종 값 대입.
            scoreText.text = FormatScore(playerFinal, aiFinal);
        }

        // 보석 점수 표기 "보석: 내 점수 vs 상대 점수"
        private string FormatScore(int player, int ai)
        {
            return "보석: " + player + " vs " + ai;
        }

        // ─────────────────────────────────────────────────────────────
        // 내부 유틸 / 버튼 핸들러
        // ─────────────────────────────────────────────────────────────

        // 종료 사유 코드를 한글 문구로 변환
        private string TranslateReason(string endReason)
        {
            switch (endReason)
            {
                case "CenterEmpty": return "중앙 보석 소진";
                case "PlayerOutOfCards": return "내 카드 소진";
                case "AiOutOfCards": return "상대 카드 소진";
                default: return endReason ?? string.Empty;
            }
        }

        // 다시 하기: 현재 Game 씬을 재로드
        private void OnRestartClicked()
        {
            SceneLoader.LoadGame();
        }

        // 메인 메뉴로 돌아가기
        private void OnMenuClicked()
        {
            SceneLoader.LoadMainMenu();
        }
    }
}
