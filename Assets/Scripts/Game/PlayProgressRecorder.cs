using UnityEngine;
using Guskapaska.Core;
using Guskapaska.Util;

namespace Guskapaska.Game
{
    /// <summary>
    /// 일반 매치가 끝나면 "플레이한 적 있음"을 기록한다.
    /// 튜토리얼 모드 매치는 기록하지 않는다 (튜토리얼 시청은 별도 플래그로 관리).
    /// 게임 로직에 비침투적 — OnMatchEnded 구독만으로 동작한다.
    /// </summary>
    public class PlayProgressRecorder : MonoBehaviour
    {
        // 매치 종료 이벤트를 받기 위한 참조.
        [SerializeField] private GameManager gameManager;

        private bool _subscribed;

        private void Start()
        {
            if (gameManager == null || gameManager.Events == null)
            {
                Debug.LogError("[PlayProgressRecorder] gameManager 가 연결되지 않았습니다.");
                return;
            }

            gameManager.Events.OnMatchEnded += HandleMatchEnded;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (_subscribed && gameManager != null && gameManager.Events != null)
            {
                gameManager.Events.OnMatchEnded -= HandleMatchEnded;
                _subscribed = false;
            }
        }

        private void HandleMatchEnded(MatchResult result)
        {
            // 튜토리얼 모드가 아닐 때만 실제 플레이로 기록한다.
            if (!GameLaunchMode.StartInTutorial)
            {
                GameSettings.MarkAsPlayed();
            }
        }
    }
}