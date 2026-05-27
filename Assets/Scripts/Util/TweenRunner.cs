using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Guskapaska.Util
{
    /// <summary>
    /// Coroutine-based tween helper. All gameplay animations route through this class.
    /// Provides key-based cancellation so re-triggering a tween on the same target safely
    /// stops the previous one before starting the new one.
    /// </summary>
    /// <remarks>
    /// Components that drive tweens via <see cref="Run"/> MUST call <see cref="CancelAll"/>
    /// from their <c>OnDisable</c> to prevent leaked coroutines and stale dictionary entries.
    /// </remarks>
    public static class TweenRunner
    {
        // ─────────────────────────────────────────────────────────────
        // 내부 자료구조
        // host(MonoBehaviour) 별로 (key → Coroutine 핸들) 매핑을 보관.
        // 같은 host에서 같은 key로 새 트윈이 시작되면 이전 코루틴을 즉시 중단한다.
        // ─────────────────────────────────────────────────────────────

        private static readonly Dictionary<MonoBehaviour, Dictionary<string, Coroutine>> _tweens
            = new Dictionary<MonoBehaviour, Dictionary<string, Coroutine>>();

        // ─────────────────────────────────────────────────────────────
        // 코루틴 실행 / 취소 API
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Run a coroutine on the given host, cancelling any previous tween that shares the same key.
        /// </summary>
        /// <param name="host">MonoBehaviour that owns the coroutine lifetime.</param>
        /// <param name="key">Identifier used for cancellation. Same key replaces previous tween.</param>
        /// <param name="routine">Inner coroutine to execute.</param>
        /// <returns>Coroutine handle, or null if host is not active.</returns>
        public static Coroutine Run(MonoBehaviour host, string key, IEnumerator routine)
        {
            if (host == null || routine == null) return null;

            // host가 비활성 상태이거나 파괴 중이면 코루틴을 시작할 수 없다.
            if (!host.isActiveAndEnabled) return null;

            // host의 딕셔너리가 없으면 생성.
            if (!_tweens.TryGetValue(host, out Dictionary<string, Coroutine> dict))
            {
                dict = new Dictionary<string, Coroutine>();
                _tweens[host] = dict;
            }

            // 같은 key의 이전 코루틴이 있으면 즉시 중단.
            if (dict.TryGetValue(key, out Coroutine previous) && previous != null)
            {
                host.StopCoroutine(previous);
            }

            // wrapper로 감싸 자연 종료 시 딕셔너리에서 자동 제거되도록 한다.
            Coroutine handle = host.StartCoroutine(WrapAndCleanup(host, key, routine));
            dict[key] = handle;
            return handle;
        }

        /// <summary>
        /// Cancel a single tween previously started with <see cref="Run"/>.
        /// Safe to call even if no tween with the given key is active.
        /// </summary>
        public static void Cancel(MonoBehaviour host, string key)
        {
            if (host == null) return;
            if (!_tweens.TryGetValue(host, out Dictionary<string, Coroutine> dict)) return;

            if (dict.TryGetValue(key, out Coroutine handle) && handle != null)
            {
                host.StopCoroutine(handle);
                dict.Remove(key);
            }
        }

        /// <summary>
        /// Cancel every tween currently associated with the given host.
        /// Call this from the host's <c>OnDisable</c> to prevent leaks.
        /// </summary>
        public static void CancelAll(MonoBehaviour host)
        {
            if (host == null) return;
            if (!_tweens.TryGetValue(host, out Dictionary<string, Coroutine> dict)) return;

            // 진행 중인 모든 코루틴 중단.
            foreach (Coroutine handle in dict.Values)
            {
                if (handle != null)
                {
                    host.StopCoroutine(handle);
                }
            }

            // 딕셔너리 자체도 제거하여 메모리 누수 방지.
            dict.Clear();
            _tweens.Remove(host);
        }

        // wrapper 코루틴: inner를 끝까지 돌린 뒤 자신을 딕셔너리에서 제거.
        private static IEnumerator WrapAndCleanup(MonoBehaviour host, string key, IEnumerator inner)
        {
            yield return inner;

            // 트윈이 정상 종료된 시점에 host가 파괴되었거나 더 이상 추적되지 않으면 그냥 종료.
            if (host == null) yield break;
            if (!_tweens.TryGetValue(host, out Dictionary<string, Coroutine> dict)) yield break;

            // 다른 트윈이 이미 같은 key를 덮어썼다면 그것은 살아있어야 하므로 건드리지 않는다.
            // 따라서 dict에서 key를 단순 제거하지 말고, "내가 종료된 이 시점에 이 키의 핸들이
            // 여전히 존재한다면" 제거하는 식으로 안전 처리한다.
            if (dict.ContainsKey(key))
            {
                // 주의: 정확히 "이 코루틴"이 등록된 것인지 식별할 방법이 없지만,
                // 새로운 트윈이 시작되었다면 이전 핸들은 이미 StopCoroutine된 후 덮어쓰기 된다.
                // 따라서 이 시점에 도달했다는 것은 자연 종료된 코루틴이고, 그 핸들이 여전히
                // 남아있다면 안전하게 제거할 수 있다.
                dict.Remove(key);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 자주 쓰이는 트윈 헬퍼들
        // 모든 헬퍼는 마지막에 정확히 to 값으로 끝나도록 보장한다 (부동소수점 오차 방지).
        // curve가 null이면 EaseOutQuad가 기본 적용된다.
        // ─────────────────────────────────────────────────────────────

        /// <summary>Interpolate <c>localPosition</c> from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static IEnumerator MoveLocal(Transform t, Vector3 from, Vector3 to, float duration, AnimationCurve curve = null)
        {
            if (t == null) yield break;

            // 곡선 기본값 적용.
            curve ??= EasingCurves.EaseOutQuad;

            // duration이 0 이하인 경우 즉시 to 값을 적용하고 종료.
            if (duration <= 0f)
            {
                t.localPosition = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float k = curve.Evaluate(u);

                // EaseOutBack 등은 1.0을 넘어가는 구간이 있으므로 LerpUnclamped 사용.
                t.localPosition = Vector3.LerpUnclamped(from, to, k);

                // 도중에 Transform이 파괴되면 안전하게 종료.
                if (t == null) yield break;

                yield return null;
            }

            // 누적 오차 방지를 위해 마지막에 정확한 to 값 대입.
            t.localPosition = to;
        }

        /// <summary>
        /// Interpolate <c>localPosition</c> from <paramref name="from"/> to <paramref name="to"/> along a parabolic arc.
        /// </summary>
        /// <param name="arcHeight">Peak height of the arc above the linear path, in local units.</param>
        public static IEnumerator MoveLocalArc(Transform t, Vector3 from, Vector3 to, float arcHeight, float duration, AnimationCurve curve = null)
        {
            if (t == null) yield break;

            curve ??= EasingCurves.EaseInOutQuad;

            if (duration <= 0f)
            {
                t.localPosition = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float k = curve.Evaluate(u);

                // 위치 보간은 곡선 적용 값 k 사용 (가감속 담당).
                Vector3 linearPos = Vector3.LerpUnclamped(from, to, k);

                // 포물선의 추가 Y 오프셋: 4 * h * u * (1 - u)
                // u = 0 또는 1 일 때 0, u = 0.5 일 때 최고점 arcHeight.
                // 여기서는 u(곡선 미적용)를 써야 포물선 자체는 균일한 호를 그린다.
                float arcOffset = 4f * arcHeight * u * (1f - u);

                t.localPosition = linearPos + new Vector3(0f, arcOffset, 0f);

                if (t == null) yield break;
                yield return null;
            }

            t.localPosition = to;
        }

        /// <summary>Interpolate <c>localScale</c> from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static IEnumerator Scale(Transform t, Vector3 from, Vector3 to, float duration, AnimationCurve curve = null)
        {
            if (t == null) yield break;

            curve ??= EasingCurves.EaseOutQuad;

            if (duration <= 0f)
            {
                t.localScale = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float k = curve.Evaluate(u);

                t.localScale = Vector3.LerpUnclamped(from, to, k);

                if (t == null) yield break;
                yield return null;
            }

            t.localScale = to;
        }

        /// <summary>Interpolate <c>localRotation</c> from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static IEnumerator Rotate(Transform t, Quaternion from, Quaternion to, float duration, AnimationCurve curve = null)
        {
            if (t == null) yield break;

            curve ??= EasingCurves.EaseOutQuad;

            if (duration <= 0f)
            {
                t.localRotation = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float k = curve.Evaluate(u);

                // Quaternion은 SlerpUnclamped로 자연스러운 구면 보간.
                t.localRotation = Quaternion.SlerpUnclamped(from, to, k);

                if (t == null) yield break;
                yield return null;
            }

            t.localRotation = to;
        }

        /// <summary>Interpolate <see cref="CanvasGroup.alpha"/> from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, AnimationCurve curve = null)
        {
            if (cg == null) yield break;

            curve ??= EasingCurves.EaseOutQuad;

            if (duration <= 0f)
            {
                cg.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float k = curve.Evaluate(u);

                cg.alpha = Mathf.LerpUnclamped(from, to, k);

                if (cg == null) yield break;
                yield return null;
            }

            cg.alpha = to;
        }
    }
}