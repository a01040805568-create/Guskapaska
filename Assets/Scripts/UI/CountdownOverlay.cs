using System.Collections;
using TMPro;
using UnityEngine;
using Guskapaska.Util;

namespace Guskapaska.UI
{
    /// <summary>
    /// Full-screen overlay that displays a single large number (3, 2, or 1) with a
    /// pop-and-fade animation each time it is triggered. Driven by GameUIController
    /// when the round timer drops to 3 seconds or below.
    /// </summary>
    public class CountdownOverlay : MonoBehaviour
    {
        [Header("Visual Refs")]
        [Tooltip("오버레이 전체의 알파/표시를 제어하는 CanvasGroup.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("카운트다운 숫자를 표시하는 TMP 텍스트.")]
        [SerializeField] private TextMeshProUGUI numberText;

        [Tooltip("숫자 텍스트가 부착된 RectTransform. 스케일 애니메이션 대상.")]
        [SerializeField] private RectTransform numberTransform;

        [Header("Animation Settings")]
        [Tooltip("카운트다운 1회 표시의 총 지속 시간(초).")]
        [SerializeField] private float showDuration = 0.7f;

        [Tooltip("시작 스케일. 1.0이면 효과 없음.")]
        [SerializeField] private float startScale = 0.5f;

        [Tooltip("종료 스케일. 시작보다 크면 확대 효과.")]
        [SerializeField] private float endScale = 1.5f;

        [Tooltip("페이드 인 비중(0~1). 전체 시간 중 앞쪽 이 비율 동안 알파 0→1.")]
        [SerializeField, Range(0f, 0.5f)] private float fadeInPortion = 0.3f;

        // 현재 진행 중인 표시 코루틴. 새 호출 시 강제 종료.
        private Coroutine _activeShow;

        private void Awake()
        {
            // 초기 상태 — 보이지 않음.
            HideInstant();
        }

        private void OnDisable()
        {
            // 진행 중인 코루틴 정리.
            if (_activeShow != null)
            {
                StopCoroutine(_activeShow);
                _activeShow = null;
            }
            TweenRunner.CancelAll(this);
        }

        /// <summary>
        /// Show the given number with a pop-and-fade animation. Calling this again
        /// while a previous animation is still running cancels the previous one
        /// and starts the new number fresh.
        /// </summary>
        public void ShowNumber(int number)
        {
            // 진행 중이던 시퀀스가 있으면 중단.
            if (_activeShow != null)
            {
                StopCoroutine(_activeShow);
                _activeShow = null;
            }

            _activeShow = StartCoroutine(RunShowSequence(number));
        }

        /// <summary>Force-hide the overlay immediately, cancelling any in-flight animation.</summary>
        public void HideInstant()
        {
            if (_activeShow != null)
            {
                StopCoroutine(_activeShow);
                _activeShow = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                // 입력은 항상 차단 (오버레이는 시각 전용).
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 내부 시퀀스
        // ─────────────────────────────────────────────────────────────

        private IEnumerator RunShowSequence(int number)
        {
            // 필수 참조 누락 시 즉시 종료.
            if (canvasGroup == null || numberText == null || numberTransform == null)
            {
                Debug.LogWarning("[CountdownOverlay] 필수 참조 누락. 카운트다운을 표시하지 못합니다.");
                yield break;
            }

            // 숫자 갱신.
            numberText.text = number.ToString();

            // 초기 상태 — 작게, 투명하게.
            numberTransform.localScale = Vector3.one * startScale;
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            AnimationCurve scaleCurve = EasingCurves.EaseOutQuad;
            // 페이드 인은 빠르게, 페이드 아웃은 부드럽게 — 단순 Linear 사용.

            while (elapsed < showDuration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / showDuration);

                // 스케일: 처음부터 끝까지 EaseOutQuad로 startScale → endScale.
                float kScale = scaleCurve.Evaluate(u);
                float currentScale = Mathf.LerpUnclamped(startScale, endScale, kScale);
                numberTransform.localScale = Vector3.one * currentScale;

                // 알파: 0 → fadeInPortion 사이에 0→1, 나머지 구간에서 1→0.
                float alpha;
                if (u < fadeInPortion)
                {
                    // 페이드 인 구간.
                    alpha = Mathf.Clamp01(u / Mathf.Max(0.0001f, fadeInPortion));
                }
                else
                {
                    // 페이드 아웃 구간.
                    float fadeOutProgress = (u - fadeInPortion) / Mathf.Max(0.0001f, 1f - fadeInPortion);
                    alpha = 1f - Mathf.Clamp01(fadeOutProgress);
                }
                canvasGroup.alpha = alpha;

                yield return null;
            }

            // 종료 시 완전히 투명.
            canvasGroup.alpha = 0f;
            _activeShow = null;
        }
    }
}
