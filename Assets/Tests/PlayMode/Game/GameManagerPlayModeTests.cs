using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Guskapaska.Core;
using Guskapaska.Game;

namespace Tests.PlayMode.Game
{
    /// <summary>
    /// End-to-end PlayMode tests for Stage 2 game flow.
    /// Loads the Game scene and verifies the full match orchestration.
    /// See Stage2_GameManager.md §"PlayMode Tests" for the spec.
    /// </summary>
    public class GameManagerPlayModeTests
    {
        private const string GameSceneName = "Game";

        // 매 테스트 실행 전 Game 씬을 로드한다.
        // SetUp은 동기 메서드라 LoadSceneAsync는 [UnitySetUp]으로 별도 작성해야 하지만,
        // 여기서는 각 테스트 본문에서 직접 로드해 흐름을 단순하게 유지.

        // 매 테스트 종료 후 정리
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // 다음 테스트가 깨끗한 상태에서 시작되도록 잠시 대기
            yield return null;
        }

        // ----- 공통 유틸 -----

        // Game 씬을 로드하고 GameManager가 초기화될 때까지 대기
        private IEnumerator LoadGameSceneAndWait()
        {
            var op = SceneManager.LoadSceneAsync(GameSceneName);
            while (!op.isDone) yield return null;
            // Awake → Start 가 모두 끝나도록 한 프레임 더 대기
            yield return null;
        }

        // 현재 씬에서 GameManager 찾기 (Unity 6 권장 API 사용)
        private GameManager FindGameManager()
        {
            return Object.FindFirstObjectByType<GameManager>();
        }

        // ----- 1. 매치 초기 상태 검증 -----

        [UnityTest]
        public IEnumerator MatchInitializesCorrectly()
        {
            yield return LoadGameSceneAndWait();

            GameManager gm = FindGameManager();
            Assert.IsNotNull(gm, "GameManager not found in scene.");
            Assert.IsNotNull(gm.State, "GameState was not initialized.");

            // 초기 핸드는 각 5장, 보석은 13개, 총 카드 10장
            Assert.AreEqual(5, gm.State.PlayerHand.Count, "Player initial hand size should be 5.");
            Assert.AreEqual(5, gm.State.AiHand.Count, "AI initial hand size should be 5.");
            Assert.AreEqual(13, gm.State.CenterGems, "Center gems should start at 13.");
            Assert.AreEqual(10, gm.State.TotalCardsInPlay(), "Total cards in play must be 10.");
        }

        // ----- 2. 플레이어 카드 제출 시 라운드 해결 -----

        [UnityTest]
        public IEnumerator SinglePlayerSubmitResolvesRound()
        {
            yield return LoadGameSceneAndWait();

            GameManager gm = FindGameManager();
            int initialRound = gm.State.RoundNumber;

            // 라운드 해결 이벤트를 받기 위한 플래그
            bool resolved = false;
            RoundOutcome captured = null;
            gm.Events.OnRoundResolved += o => { resolved = true; captured = o; };

            // 플레이어가 핸드의 첫 카드를 제출
            Card playerCard = gm.State.PlayerHand.GetAt(0);
            gm.OnPlayerSubmit(playerCard);

            // 라운드 해결까지 잠시 대기 (코루틴 한 사이클)
            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(resolved, "Round was not resolved after player submitted.");
            Assert.IsNotNull(captured, "RoundOutcome should not be null.");
            Assert.AreEqual(playerCard.Id, captured.PlayerCard.Id, "Player card in outcome should match submitted.");
            Assert.Greater(gm.State.RoundNumber, initialRound, "Round number should have incremented.");
        }

        // ----- 3. 타이머 만료 시 자동 제출 -----

        [UnityTest]
        public IEnumerator TimerExpiryAutoSubmits()
        {
            yield return LoadGameSceneAndWait();

            GameManager gm = FindGameManager();

            bool resolved = false;
            gm.Events.OnRoundResolved += _ => resolved = true;

            // 라운드 시간(15초) + 약간의 여유. 너무 길어지는 걸 피하려면
            // 테스트용으로 MatchConfig의 RoundTimeSeconds를 짧게 설정하는 게 이상적이지만,
            // 여기서는 기본값을 신뢰하고 단순 대기.
            yield return new WaitForSeconds(16f);

            Assert.IsTrue(resolved, "Round should auto-resolve when timer expires.");
        }

        // ----- 4. 무승부 시 누적 코인이 증가하는지 -----

        [UnityTest]
        public IEnumerator DrawAccumulates()
        {
            yield return LoadGameSceneAndWait();

            GameManager gm = FindGameManager();

            // 양쪽 핸드를 모두 같은 모양 카드로 강제 → 100% 무승부 보장
            // Stage 1 CardFactory의 표준 12장 셋에서 Rock 3장 + 추가로 똑같이 만들 수는 없으므로
            // 직접 Card 인스턴스를 생성한다. (테스트 전용 카드 ID로 구분)
            var playerCards = new List<Card>
            {
                new Card("TEST_R_P_1", CardShape.Rock, 1),
                new Card("TEST_R_P_2", CardShape.Rock, 2),
                new Card("TEST_R_P_3", CardShape.Rock, 0),
                new Card("TEST_R_P_4", CardShape.Rock, 1),
                new Card("TEST_R_P_5", CardShape.Rock, 0),
            };
            var aiCards = new List<Card>
            {
                new Card("TEST_R_A_1", CardShape.Rock, 1),
                new Card("TEST_R_A_2", CardShape.Rock, 2),
                new Card("TEST_R_A_3", CardShape.Rock, 0),
                new Card("TEST_R_A_4", CardShape.Rock, 1),
                new Card("TEST_R_A_5", CardShape.Rock, 0),
            };

            TestHelper.OverrideBothHands(gm.State, playerCards, aiCards);

            int drawCoinsBefore = gm.State.AccumulatedDrawCoins;
            int stashBefore = gm.State.DrawStash.Count;

            // 플레이어 카드 제출 → AI도 Rock만 있으니 무승부 확정
            gm.OnPlayerSubmit(gm.State.PlayerHand.GetAt(0));
            yield return new WaitForSeconds(0.3f);

            Assert.Greater(gm.State.AccumulatedDrawCoins, drawCoinsBefore,
                "Accumulated draw coins should increase after a draw round.");
            Assert.AreEqual(stashBefore + 2, gm.State.DrawStash.Count,
                "Draw stash should grow by exactly 2 cards (player + AI) after one draw.");
        }

        // ----- 5. 중앙 보석이 비어버릴 때 매치 종료 -----

        [UnityTest]
        public IEnumerator MatchEndsWhenCenterEmpty()
        {
            yield return LoadGameSceneAndWait();

            GameManager gm = FindGameManager();

            // 매치 종료 이벤트 수신용
            MatchResult result = null;
            gm.Events.OnMatchEnded += r => result = r;

            // 보석 수를 강제로 0에 가깝게 만들기 위해, 중앙을 거의 비운다.
            // GameState는 외부 setter가 없으므로 AddPlayerGems로 빼낸다.
            // 13개 중 12개를 플레이어에게 미리 옮겨놓아 사실상 1개만 남게 한다.
            gm.State.AddPlayerGems(12);

            // 플레이어 카드 제출 → 라운드 해결 시 outcome.CoinsAwarded 가 1 이상이면
            // 중앙이 0이 되어 매치 종료로 이어진다.
            // 단, 패배하면 중앙은 그대로다. 일단 한 라운드 끝나길 기다린 뒤,
            // 종료가 안 됐다면 다음 라운드까지 추가 진행.
            int safetyRounds = 10;
            while (result == null && safetyRounds-- > 0)
            {
                if (gm.State.PlayerHand.IsEmpty || gm.State.AiHand.IsEmpty) break;
                gm.OnPlayerSubmit(gm.State.PlayerHand.GetAt(0));
                // 라운드 해결 + ResultDelay 대기
                yield return new WaitForSeconds(2.0f);
            }

            Assert.IsNotNull(result, "Match should have ended after a few rounds.");
            // 종료 사유는 CenterEmpty 또는 핸드 소진 중 하나여야 함.
            Assert.IsTrue(
                result.EndReason == "CenterEmpty" ||
                result.EndReason == "PlayerOutOfCards" ||
                result.EndReason == "AiOutOfCards",
                $"Unexpected end reason: {result.EndReason}");
        }

        // ----- 6. 핸드가 비면 매치 종료 -----

        [UnityTest]
        public IEnumerator MatchEndsWhenHandEmpty()
        {
            yield return LoadGameSceneAndWait();

            GameManager gm = FindGameManager();

            MatchResult result = null;
            gm.Events.OnMatchEnded += r => result = r;

            // 플레이어 핸드를 비우려면 IsGameOver 조건상 라운드가 끝나는 시점에 0이어야 함.
            // 양쪽 핸드를 1장으로 줄여놓고, 무승부가 일어나면 두 장 모두 스택으로 가서 0이 됨.
            var playerCards = new List<Card> { new Card("TEST_HE_P_1", CardShape.Rock, 1) };
            var aiCards = new List<Card> { new Card("TEST_HE_A_1", CardShape.Rock, 1) };
            TestHelper.OverrideBothHands(gm.State, playerCards, aiCards);

            // 무승부 → 양쪽 카드 모두 스택으로 이동 → 양쪽 핸드 0장 → IsGameOver
            gm.OnPlayerSubmit(gm.State.PlayerHand.GetAt(0));

            // 결과 대기 (ResultDelaySeconds + 여유)
            yield return new WaitForSeconds(2.5f);

            Assert.IsNotNull(result, "Match should have ended due to empty hands.");
            Assert.IsTrue(
                result.EndReason == "PlayerOutOfCards" ||
                result.EndReason == "AiOutOfCards",
                $"Unexpected end reason: {result.EndReason}");
        }

        // ----- 7. 카드 보존 법칙 (총합 10) -----

        [UnityTest]
        public IEnumerator CardConservation()
        {
            yield return LoadGameSceneAndWait();

            GameManager gm = FindGameManager();

            // 매 라운드 해결 직후 총합이 10인지 확인
            bool violated = false;
            int observedTotal = -1;
            gm.Events.OnRoundResolved += _ =>
            {
                int total = gm.State.TotalCardsInPlay();
                if (total != 10)
                {
                    violated = true;
                    observedTotal = total;
                }
            };

            // 몇 라운드를 자동 진행시키며 관찰
            int safetyRounds = 5;
            while (safetyRounds-- > 0)
            {
                if (gm.State.PlayerHand.IsEmpty || gm.State.AiHand.IsEmpty) break;
                if (gm.State.CenterGems == 0) break;

                gm.OnPlayerSubmit(gm.State.PlayerHand.GetAt(0));
                yield return new WaitForSeconds(2.0f);
            }

            Assert.IsFalse(violated,
                $"Card conservation violated: observed total={observedTotal}, expected 10.");
        }
    }
}