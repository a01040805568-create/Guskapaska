using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Guskapaska.Core;
using Guskapaska.Util;

namespace Guskapaska.Game
{
    /// <summary>
    /// Top-level match orchestrator. Owns GameState, GameEvents, and the RNG.
    /// Sets up the deck, deals hands, runs rounds via RoundController,
    /// and ends the match when win conditions are met.
    /// See 00_GameDesign.md §4-§7 for the rules this class enforces.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private MatchConfig config;

        [Header("Controllers")]
        [SerializeField] private TimerController timerController;
        [SerializeField] private RoundController roundController;

        [Header("Tutorial")]
        // false면 씬 진입 시 자동 매치를 시작하지 않는다. 튜토리얼은 외부에서 StartTutorialMatch를 호출.
        [SerializeField] private bool autoStartMatch = true;

        /// <summary>Event bus other components can subscribe to.</summary>
        public GameEvents Events { get; private set; }

        /// <summary>Live state of the current match.</summary>
        public GameState State { get; private set; }

        // 내부 의존성
        private System.Random _rng;
        private IAiStrategy _aiStrategy;
        private bool _matchActive;

        private void Awake()
        {
            // 직렬화 필드 검증 (인스펙터에서 빠뜨린 경우를 빨리 잡기 위함)
            if (config == null)
                Debug.LogError("[GameManager] MatchConfig is not assigned in the Inspector.");
            if (timerController == null)
                Debug.LogError("[GameManager] TimerController reference is missing.");
            if (roundController == null)
                Debug.LogError("[GameManager] RoundController reference is missing.");

            // 이벤트 버스 생성
            Events = new GameEvents();

            // RNG 시드 결정 (0 = 시간 기반, 그 외 = 고정)
            int seed = (config != null && config.Seed != 0) ? config.Seed : Environment.TickCount;
            _rng = new System.Random(seed);

            // AI 전략 생성 (랜덤 선택, Stage 1의 클래스)
            _aiStrategy = new AiRandomStrategy(_rng);
        }

        private void Start()
        {
            // 튜토리얼 모드면 TutorialController가 매치를 시작하므로 자동 시작하지 않는다.
            if (!GameLaunchMode.StartInTutorial && autoStartMatch)
            {
                StartMatch();
            }
        }

        /// <summary>
        /// 표준 덱을 셔플·분배하여 일반 매치를 시작한다.
        /// </summary>
        public void StartMatch()
        {
         State = new GameState(config.TotalGems);

         // 표준 12장 덱 생성 → 셔플 → 5장씩 분배 (남은 2장은 제외)
         List<Card> standardSet = CardFactory.CreateStandardSet();
         Deck deck = new Deck(standardSet);
         deck.Shuffle(_rng);

         State.PlayerHand.AddRange(deck.DrawTop(config.InitialHandSize));
         State.AiHand.AddRange(deck.DrawTop(config.InitialHandSize));

         BeginMatch(_aiStrategy);
        }

        /// <summary>
        /// 튜토리얼용 매치를 시작한다. 셔플 대신 지정된 손패를 그대로 사용하고,
        /// 지정된 AI 전략(보통 ScriptedAiStrategy)을 주입한다.
        /// </summary>
        public void StartTutorialMatch(List<Card> playerCards, List<Card> aiCards, IAiStrategy aiStrategy)
        {
          State = new GameState(config.TotalGems);
         State.PlayerHand.AddRange(playerCards);
         State.AiHand.AddRange(aiCards);

         BeginMatch(aiStrategy);
        }

        // 손패 구성이 끝난 뒤 컨트롤러 주입·이벤트 발생·첫 라운드 시작을 처리하는 공통 로직.
         private void BeginMatch(IAiStrategy strategy)
        {
         timerController.Initialize(Events, config);
         roundController.Initialize(State, Events, config, timerController, strategy, _rng);

         Events.OnRoundResolved += HandleRoundResolved;

         Events.RaiseMatchStarted(State);
         Events.RaiseGemsChanged(State.PlayerGems, State.AiGems, State.CenterGems);

         _matchActive = true;

        roundController.StartRound();
        }

        /// <summary>
        /// Convenience pass-through for UI/test code to submit the player's card.
        /// </summary>
        public void OnPlayerSubmit(Card card)
        {
            roundController.SubmitPlayerCard(card);
        }

        // 라운드가 끝났을 때 호출됨. 다음 라운드를 시작할지 매치를 종료할지 결정.
        private void HandleRoundResolved(RoundOutcome outcome)
        {
            if (!_matchActive) return;

            StartCoroutine(EndOfRoundRoutine());
        }

        // 라운드 결과 표시 시간만큼 기다린 뒤 다음 라운드 or 매치 종료 처리
        private IEnumerator EndOfRoundRoutine()
        {
            // 결과를 사용자가 인지할 시간을 줌 (UI가 들어오면 의미가 큼)
            yield return new WaitForSeconds(config.ResultDelaySeconds);

            if (State.IsGameOver())
            {
                // 종료 사유 결정 (우선순위 순서)
                string reason;
                if (State.CenterGems == 0)
                    reason = "CenterEmpty";
                else if (State.PlayerHand.IsEmpty)
                    reason = "PlayerOutOfCards";
                else
                    reason = "AiOutOfCards";

                EndMatch(reason);
            }
            else
            {
                // 다음 라운드 진행
                roundController.StartRound();
            }
        }

        // 매치를 종료하고 최종 결과 이벤트를 발생시킴
        private void EndMatch(string reason)
        {
            // 안전하게 타이머 정지
            if (timerController != null) timerController.StopTimer();

            // 보석 수로 최종 승자 판정
            MatchWinner winner;
            if (State.PlayerGems > State.AiGems) winner = MatchWinner.Player;
            else if (State.AiGems > State.PlayerGems) winner = MatchWinner.Ai;
            else winner = MatchWinner.Tie;

            MatchResult result = new MatchResult(
                winner,
                State.PlayerGems,
                State.AiGems,
                State.RoundNumber,
                reason);

            _matchActive = false;
            Events.RaiseMatchEnded(result);
        }

        private void OnDestroy()
        {
            // 메모리 누수 방지: 구독 해제
            if (Events != null)
                Events.OnRoundResolved -= HandleRoundResolved;
        }
    }
}