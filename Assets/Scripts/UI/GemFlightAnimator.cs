using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Guskapaska.Util;

namespace Guskapaska.UI
{
    /// <summary>
    /// Orchestrates the gem acquisition animation:
    /// rise → Mario hand descends → hand pauses → hand ascends with gems → gems fly to pile.
    /// Game logic for gem counts is unaffected; only the visual catches up.
    /// </summary>
    public class GemFlightAnimator : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("비행 보석 프리팹. 작은 시안 사각형 + Image 컴포넌트.")]
        [SerializeField] private GameObject gemPrefab;

        [Tooltip("마리오 손 프리팹. 갈색 placeholder.")]
        [SerializeField] private GameObject marioHandPrefab;

        [Header("Anchors")]
        [Tooltip("플레이어의 GemPile 위치 (도착 지점).")]
        [SerializeField] private RectTransform playerGemTarget;

        [Tooltip("AI의 GemPile 위치 (도착 지점).")]
        [SerializeField] private RectTransform aiGemTarget;

        [Tooltip("중앙 보석 그리드의 위치 출처. CoinGridView 참조.")]
        [SerializeField] private CoinGridView coinGridView;

        [Tooltip("비행 보석과 손이 잠시 살 부모. 보통 메인 Canvas의 RectTransform.")]
        [SerializeField] private RectTransform animationContainer;

        [Header("Hand Descent")]
        [Tooltip("마리오 손이 화면 위쪽에서 내려오는 시작 Y 오프셋 (보석 위치 기준 상대값, 픽셀).")]
        [SerializeField] private float handStartYOffset = 300f;

        [Header("Timings (seconds)")]
        [SerializeField] private float gemRiseHeight = 30f;       // 보석이 솟구치는 높이
        [SerializeField] private float gemRiseDuration = 0.2f;
        [SerializeField] private float handDescendDuration = 0.3f;
        [SerializeField] private float handPauseDuration = 0.15f;
        [SerializeField] private float handAscendDuration = 0.3f;
        [SerializeField] private float gemFlightDuration = 0.5f;
        [SerializeField] private float gemFlightArcHeight = 80f;

        // 현재 진행 중인 시퀀스의 코루틴. 새 호출이 들어오면 강제 종료한다.
        private Coroutine _activeSequence;

        // 살아있는 임시 인스턴스들. OnDisable 시 일괄 정리.
        private readonly List<GameObject> _spawnedTemporaries = new List<GameObject>();

        private void OnDisable()
        {
            // 진행 중인 시퀀스 강제 정리.
            if (_activeSequence != null)
            {
                StopCoroutine(_activeSequence);
                _activeSequence = null;
            }

            DestroyAllSpawned();
            TweenRunner.CancelAll(this);
        }

        /// <summary>
        /// Animate <paramref name="count"/> gems flying from the rightmost filled cells
        /// of the center grid to the specified winner's gem pile. On arrival, calls the
        /// onArrived callback so the caller can update GemPileView and CoinGridView.
        /// </summary>
        public void StartGemAcquisition(int count, bool winnerIsPlayer, System.Action onArrived)
        {
            // 카운트가 0이면 즉시 도착 처리.
            if (count <= 0)
            {
                onArrived?.Invoke();
                return;
            }

            // 이전 시퀀스가 아직 진행 중이면 강제 종료.
            // 이론상 라운드 사이에는 충분한 시간이 있어 거의 발생하지 않지만 안전망.
            if (_activeSequence != null)
            {
                StopCoroutine(_activeSequence);
                DestroyAllSpawned();
            }

            _activeSequence = StartCoroutine(RunSequence(count, winnerIsPlayer, onArrived));
        }

        private IEnumerator RunSequence(int count, bool winnerIsPlayer, System.Action onArrived)
        {
            // 필수 참조 누락 시 폴백.
            if (gemPrefab == null || coinGridView == null || animationContainer == null)
            {
                Debug.LogWarning("[GemFlightAnimator] 필수 참조 누락으로 즉시 도착 처리됩니다.");
                onArrived?.Invoke();
                _activeSequence = null;
                yield break;
            }

            RectTransform targetRt = winnerIsPlayer ? playerGemTarget : aiGemTarget;
            if (targetRt == null)
            {
                Debug.LogWarning("[GemFlightAnimator] 도착 지점이 연결되지 않았습니다.");
                onArrived?.Invoke();
                _activeSequence = null;
                yield break;
            }

            // 1) 중앙 그리드의 가장 오른쪽 채워진 셀들의 월드 좌표를 가져온다.
            List<Vector3> startWorlds = coinGridView.GetRightmostFilledCellPositions(count);
            if (startWorlds.Count == 0)
            {
                // 채워진 셀이 없는데 보석을 가져가는 경우 (예외 케이스). 즉시 도착.
                onArrived?.Invoke();
                _activeSequence = null;
                yield break;
            }

            // 2) 각 시작 위치마다 비행 보석 인스턴스 생성.
            //    container 기준 로컬 좌표로 변환하여 배치.
            List<RectTransform> gemRts = new List<RectTransform>();
            for (int i = 0; i < startWorlds.Count; i++)
            {
                GameObject gemGo = Instantiate(gemPrefab, animationContainer);
                _spawnedTemporaries.Add(gemGo);

                RectTransform rt = gemGo.GetComponent<RectTransform>();
                if (rt == null)
                {
                    Destroy(gemGo);
                    continue;
                }

                Vector3 startLocal = animationContainer.InverseTransformPoint(startWorlds[i]);
                rt.localPosition = startLocal;
                rt.localScale = Vector3.one;

                gemRts.Add(rt);
            }

            if (gemRts.Count == 0)
            {
                onArrived?.Invoke();
                _activeSequence = null;
                yield break;
            }

            // 3) 보석 솟구침 — 살짝 위로.
            //    모든 보석을 동시에 띄우므로 각 보석마다 코루틴을 동시에 시작하고
            //    가장 긴 시간이 끝날 때까지 대기.
            List<Vector3> riseTargets = new List<Vector3>();
            foreach (RectTransform rt in gemRts)
            {
                Vector3 risen = rt.localPosition + new Vector3(0f, gemRiseHeight, 0f);
                riseTargets.Add(risen);
            }

            yield return AnimateAll(gemRts, riseTargets, gemRiseDuration, EasingCurves.EaseOutQuad);

            // 4) 마리오 손 등장 + 보석 위로 내려옴.
            //    손은 보석들의 중앙 위치에서 위로 handStartYOffset 만큼 떨어진 곳에서 시작.
            GameObject handGo = null;
            RectTransform handRt = null;
            Vector3 handFinalLocalPos = Vector3.zero;

            if (marioHandPrefab != null)
            {
                handGo = Instantiate(marioHandPrefab, animationContainer);
                _spawnedTemporaries.Add(handGo);

                handRt = handGo.GetComponent<RectTransform>();
                if (handRt != null)
                {
                    // 보석들의 평균 위치 = 손의 최종 위치.
                    Vector3 avg = Vector3.zero;
                    foreach (RectTransform rt in gemRts)
                    {
                        avg += rt.localPosition;
                    }
                    avg /= gemRts.Count;
                    handFinalLocalPos = avg;

                    Vector3 handStart = handFinalLocalPos + new Vector3(0f, handStartYOffset, 0f);
                    handRt.localPosition = handStart;
                    handRt.localScale = Vector3.one;

                    // 손 내려오는 트윈.
                    yield return TweenRunner.MoveLocal(handRt, handStart, handFinalLocalPos, handDescendDuration, EasingCurves.EaseOutQuad);
                }
            }

            // 5) 손이 잠시 멈춤 — 동시에 보석은 손 뒤에 가려져 있는 상태.
            //    단순히 시간만 대기.
            if (handGo != null && handPauseDuration > 0f)
            {
                yield return new WaitForSeconds(handPauseDuration);
            }

            // 6) 손이 위로 사라지며 보석도 비행 시작 (동시 진행).
            //    손의 페이드아웃은 단순히 올라가는 것으로 표현 (별도 알파 페이드 안 함).
            if (handRt != null)
            {
                Vector3 handExit = handFinalLocalPos + new Vector3(0f, handStartYOffset, 0f);
                // 손 트윈은 별도로 띄워두고, 메인 흐름은 보석 비행에 집중.
                StartCoroutine(MoveAndDestroyHand(handRt, handFinalLocalPos, handExit, handGo));
            }

            // 7) 보석 비행 시작 — 도착 지점은 targetRt의 월드 좌표를 container 로컬로 변환.
            Vector3 endWorld = targetRt.position;
            Vector3 endLocal = animationContainer.InverseTransformPoint(endWorld);

            List<IEnumerator> flightRoutines = new List<IEnumerator>();
            foreach (RectTransform rt in gemRts)
            {
                Vector3 startLocal = rt.localPosition;
                flightRoutines.Add(ArcFlight(rt, startLocal, endLocal, gemFlightArcHeight, gemFlightDuration));
            }

            // 모든 비행 트윈을 동시에 시작하고 가장 늦은 것이 끝날 때까지 대기.
            yield return RunAllParallel(flightRoutines);

            // 8) 도착 — 비행 보석 인스턴스들을 정리.
            foreach (RectTransform rt in gemRts)
            {
                if (rt != null && rt.gameObject != null)
                {
                    _spawnedTemporaries.Remove(rt.gameObject);
                    Destroy(rt.gameObject);
                }
            }

            // 9) 콜백 — GemPileView 카운트 갱신, CoinGridView 갱신은 여기서.
            onArrived?.Invoke();

            _activeSequence = null;
        }

        // 손이 위로 사라진 후 GameObject 파괴.
        private IEnumerator MoveAndDestroyHand(RectTransform handRt, Vector3 from, Vector3 to, GameObject handGo)
        {
            yield return TweenRunner.MoveLocal(handRt, from, to, handAscendDuration, EasingCurves.EaseInQuad);

            if (handGo != null)
            {
                _spawnedTemporaries.Remove(handGo);
                Destroy(handGo);
            }
        }

        // 여러 RectTransform을 같은 시간 동안 각자의 목표 위치로 동시에 이동.
        private IEnumerator AnimateAll(List<RectTransform> rts, List<Vector3> targets, float duration, AnimationCurve curve)
        {
            if (rts.Count == 0) yield break;

            // 모든 보석의 시작 위치 보관.
            List<Vector3> starts = new List<Vector3>();
            for (int i = 0; i < rts.Count; i++)
            {
                starts.Add(rts[i].localPosition);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float k = curve.Evaluate(u);

                for (int i = 0; i < rts.Count; i++)
                {
                    if (rts[i] == null) continue;
                    rts[i].localPosition = Vector3.LerpUnclamped(starts[i], targets[i], k);
                }

                yield return null;
            }

            // 최종 위치 보장.
            for (int i = 0; i < rts.Count; i++)
            {
                if (rts[i] == null) continue;
                rts[i].localPosition = targets[i];
            }
        }

        // 단일 RectTransform의 포물선 비행.
        private IEnumerator ArcFlight(RectTransform rt, Vector3 startLocal, Vector3 endLocal, float arcHeight, float duration)
        {
            if (rt == null) yield break;

            float elapsed = 0f;
            AnimationCurve curve = EasingCurves.EaseInOutQuad;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float k = curve.Evaluate(u);

                Vector3 linear = Vector3.LerpUnclamped(startLocal, endLocal, k);
                float arc = 4f * arcHeight * u * (1f - u);
                rt.localPosition = linear + new Vector3(0f, arc, 0f);

                if (rt == null) yield break;
                yield return null;
            }

            if (rt != null)
            {
                rt.localPosition = endLocal;
            }
        }

        // 여러 코루틴을 동시에 시작하고 모두 끝날 때까지 대기.
        private IEnumerator RunAllParallel(List<IEnumerator> routines)
        {
            int remaining = routines.Count;
            if (remaining == 0) yield break;

            foreach (IEnumerator r in routines)
            {
                StartCoroutine(WrapAndCount(r, () => remaining--));
            }

            while (remaining > 0)
            {
                yield return null;
            }
        }

        // 코루틴을 감싸서 종료 시 카운터 감소 콜백 호출.
        private IEnumerator WrapAndCount(IEnumerator inner, System.Action onDone)
        {
            yield return inner;
            onDone?.Invoke();
        }

        // 살아있는 임시 인스턴스들 일괄 파괴.
        private void DestroyAllSpawned()
        {
            foreach (GameObject go in _spawnedTemporaries)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }
            _spawnedTemporaries.Clear();
        }
    }
}
