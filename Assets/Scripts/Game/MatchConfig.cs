using UnityEngine;

namespace Guskapaska.Game
{
    /// <summary>
    /// Tunable match parameters. See 00_GameDesign.md for the design intent
    /// and 01_ProjectOverview.md for namespace conventions.
    /// </summary>
    [CreateAssetMenu(fileName = "MatchConfig", menuName = "Guskapaska/Match Config")]
    public class MatchConfig : ScriptableObject
    {
        // 핸드 및 보석 초기 설정
        [Header("Hand Setup")]
        [SerializeField] private int initialHandSize = 5;       // 각 플레이어 초기 카드 수
        [SerializeField] private int totalGems = 13;            // 중앙 보석 더미 시작값

        // 라운드 타이밍 관련 설정
        [Header("Round Timing")]
        [SerializeField] private float roundTimeSeconds = 15f;       // 한 라운드 제한 시간
        [SerializeField] private float countdownStartSeconds = 3f;   // 3-2-1 카운트다운 시작 시점
        [SerializeField] private float resultDelaySeconds = 1.5f;    // 라운드 결과 표시 후 다음 라운드까지 지연

        // 랜덤 시드 (0 = 시간 기반 시드, 그 외 = 고정 시드로 재현 가능)
        [Header("Random")]
        [SerializeField] private int seed = 0;

        /// <summary>Initial number of cards dealt to each player.</summary>
        public int InitialHandSize => initialHandSize;

        /// <summary>Starting number of gems in the center pile.</summary>
        public int TotalGems => totalGems;

        /// <summary>Round time limit in seconds.</summary>
        public float RoundTimeSeconds => roundTimeSeconds;

        /// <summary>Seconds remaining when the 3-2-1 countdown event fires.</summary>
        public float CountdownStartSeconds => countdownStartSeconds;

        /// <summary>Delay between round resolution and the next round start.</summary>
        public float ResultDelaySeconds => resultDelaySeconds;

        /// <summary>RNG seed. 0 means use a time-based seed.</summary>
        public int Seed => seed;
    }
}