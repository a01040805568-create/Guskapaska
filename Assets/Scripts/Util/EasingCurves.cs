using UnityEngine;

namespace Guskapaska.Util
{
    /// <summary>
    /// Cached <see cref="AnimationCurve"/> instances for common easing functions.
    /// Curves are constructed once and reused, avoiding per-tween allocations.
    /// </summary>
    public static class EasingCurves
    {
        // ─────────────────────────────────────────────────────────────
        // 백킹 필드 — 최초 접근 시 1회 생성되어 정적으로 캐싱된다.
        // ─────────────────────────────────────────────────────────────

        private static AnimationCurve _linear;
        private static AnimationCurve _easeOutQuad;
        private static AnimationCurve _easeInQuad;
        private static AnimationCurve _easeInOutQuad;
        private static AnimationCurve _easeOutBack;
        private static AnimationCurve _easeOutBounce;

        // ─────────────────────────────────────────────────────────────
        // Public 프로퍼티
        // ─────────────────────────────────────────────────────────────

        /// <summary>Linear interpolation. f(t) = t.</summary>
        public static AnimationCurve Linear => _linear ??= AnimationCurve.Linear(0f, 0f, 1f, 1f);

        /// <summary>Ease-out quadratic. Default curve for card motion.</summary>
        public static AnimationCurve EaseOutQuad => _easeOutQuad ??= BuildEaseOutQuad();

        /// <summary>Ease-in quadratic. Slow start, fast finish.</summary>
        public static AnimationCurve EaseInQuad => _easeInQuad ??= BuildEaseInQuad();

        /// <summary>Ease-in-out quadratic. Smooth acceleration and deceleration.</summary>
        public static AnimationCurve EaseInOutQuad => _easeInOutQuad ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        /// <summary>Ease-out with a slight overshoot past 1.0 then settles back. Used for drop-fail return.</summary>
        public static AnimationCurve EaseOutBack => _easeOutBack ??= BuildEaseOutBack();

        /// <summary>Ease-out bouncing curve. Used for gem arrival.</summary>
        public static AnimationCurve EaseOutBounce => _easeOutBounce ??= BuildEaseOutBounce();

        // ─────────────────────────────────────────────────────────────
        // 곡선 빌더 — Unity 빌트인이 없는 곡선들을 Keyframe 배열로 정의
        // ─────────────────────────────────────────────────────────────

        private static AnimationCurve BuildEaseOutQuad()
        {
            // Ease-out 2차 곡선: 빠른 시작, 부드러운 마감.
            // f(t) = 1 - (1 - t)^2 의 근사. Keyframe의 접선 값으로 곡선 형태를 결정한다.
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 2f),    // 시작점 — 외향 접선 2 (빠르게 시작)
                new Keyframe(1f, 1f, 0f, 0f)     // 종점 — 외향 접선 0 (부드럽게 안착)
            );
        }

        private static AnimationCurve BuildEaseInQuad()
        {
            // Ease-in 2차 곡선: 느린 시작, 가속.
            // f(t) = t^2 의 근사.
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),    // 시작점 — 외향 접선 0 (천천히 시작)
                new Keyframe(1f, 1f, 2f, 0f)     // 종점 — 내향 접선 2 (가속하며 도달)
            );
        }

        private static AnimationCurve BuildEaseOutBack()
        {
            // Ease-out-back: 1.0을 살짝 넘어 오버슈트 후 다시 1.0으로 안착.
            // 카드가 원래 자리로 복귀할 때 "톡" 튕기는 느낌을 줌.
            // 표준 ease-out-back은 t=0.7 부근에서 약 1.1 정도까지 오버슈트.
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f,    0f,    0f, 4f),     // 빠른 출발
                new Keyframe(0.7f,  1.1f,  0f, 0f),     // 오버슈트 정점
                new Keyframe(1f,    1f,   -0.5f, 0f)    // 살짝 되돌아오며 안착
            );
            return curve;
        }

        private static AnimationCurve BuildEaseOutBounce()
        {
            // Ease-out-bounce: 3회 통통 튀며 안착.
            // 보석이 더미에 도착할 때 사용. 키프레임 6개로 튕김 표현.
            return new AnimationCurve(
                new Keyframe(0f,    0f,    0f,  5f),
                new Keyframe(0.40f, 1f,    0f, -3f),    // 1차 도달
                new Keyframe(0.55f, 0.75f, 0f,  3f),    // 1차 반동
                new Keyframe(0.75f, 1f,    0f, -1.5f),  // 2차 도달
                new Keyframe(0.85f, 0.92f, 0f,  1.5f),  // 2차 반동
                new Keyframe(1f,    1f,    0f,  0f)     // 최종 안착
            );
        }
    }
}