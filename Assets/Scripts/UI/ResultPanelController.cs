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
    /// </summary>
    public class ResultPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI titleText;       // "승리!" / "패배" / "무승부"
        [SerializeField] private TextMeshProUGUI scoreText;       // "보석: 8 vs 5"
        [SerializeField] private TextMeshProUGUI reasonText;      // "중앙 보석 소진" 등
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            // 시작 시 패널 비활성화
            if (panel != null) panel.SetActive(false);

            // 버튼 리스너 등록 (씬 내내 유효)
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenuClicked);
        }

        /// <summary>
        /// Display the result panel populated from a MatchResult.
        /// </summary>
        public void Show(MatchResult result)
        {
            if (panel == null || result == null) return;

            // 패널 활성화
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

            // 보석 점수 표기 "보석: 내 점수 vs 상대 점수"
            if (scoreText != null)
            {
                scoreText.text = "보석: " + result.PlayerGems + " vs " + result.AiGems;
            }

            // 종료 사유를 한글로 변환
            if (reasonText != null)
            {
                reasonText.text = TranslateReason(result.EndReason);
            }
        }

        /// <summary>
        /// Hide the result panel.
        /// </summary>
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        // 종료 사유 코드를 한글 문구로 변환
        private string TranslateReason(string endReason)
        {
            switch (endReason)
            {
                case "CenterEmpty":      return "중앙 보석 소진";
                case "PlayerOutOfCards": return "내 카드 소진";
                case "AiOutOfCards":     return "상대 카드 소진";
                default:                 return endReason ?? string.Empty;
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