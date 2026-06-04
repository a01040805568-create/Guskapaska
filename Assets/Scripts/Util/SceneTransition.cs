using System;
using System.Collections;
using UnityEngine;

namespace Guskapaska.Util
{
    /// <summary>
    /// A simple per-scene fader placed on top of the scene Canvas. Fades a black
    /// CanvasGroup in (on scene start) and out (before a scene load), reusing the
    /// shared <see cref="TweenRunner"/> easing utilities.
    /// </summary>
    /// <remarks>
    /// The fader's <see cref="CanvasGroup"/> should sit on a full-screen black Image
    /// that is the last child of the top Canvas, so it draws above all other UI.
    /// Keep the GameObject active and start at alpha 1; this component drives alpha
    /// and raycast-blocking so clicks pass through once a fade-in completes.
    /// </remarks>
    public class SceneTransition : MonoBehaviour
    {
        [Tooltip("검은 화면 (알파 0~1). 전체 화면 stretch Image 위의 CanvasGroup.")]
        [SerializeField] private CanvasGroup fader;
        [SerializeField] private float fadeDuration = 0.4f;

        [Tooltip("씬 시작 시 자동으로 FadeIn을 실행할지 여부. 각 씬의 fader는 보통 켜둔다.")]
        [SerializeField] private bool fadeInOnStart = true;

        private void Start()
        {
            // 각 씬의 SceneTransition이 자기 자신을 호스트로 fade-in을 실행한다.
            // (씬마다 컨트롤러에서 별도로 호출할 필요 없음)
            if (fadeInOnStart)
            {
                StartCoroutine(FadeIn());
            }
        }

        private void OnDisable()
        {
            // 진행 중인 트윈 취소 (신규 컴포넌트 규칙).
            TweenRunner.CancelAll(this);
        }

        /// <summary>Fade from black to clear on scene start, then let clicks pass through.</summary>
        public IEnumerator FadeIn()
        {
            if (fader == null) yield break;

            // 시작: 완전히 검은 화면 + 입력 차단.
            SetFaderState(1f, blockRaycasts: true);

            // 알파 1 → 0 (EaseOutQuad).
            yield return TweenRunner.FadeCanvasGroup(fader, 1f, 0f, fadeDuration, EasingCurves.EaseOutQuad);

            // 종료: 투명 + 입력 통과. 검은 화면이 남아 클릭을 막는 함정 방지.
            SetFaderState(0f, blockRaycasts: false);
        }

        /// <summary>Fade from clear to black, then invoke onComplete (typically a scene load).</summary>
        public IEnumerator FadeOut(Action onComplete)
        {
            if (fader == null)
            {
                // fader가 없으면 페이드 없이 콜백만 실행 (안전 폴백).
                onComplete?.Invoke();
                yield break;
            }

            // 시작: 투명 + 입력 차단 (페이드 동안 추가 클릭 방지).
            SetFaderState(0f, blockRaycasts: true);

            // 알파 0 → 1 (EaseInQuad).
            yield return TweenRunner.FadeCanvasGroup(fader, 0f, 1f, fadeDuration, EasingCurves.EaseInQuad);

            // 검은 화면을 유지한 채 콜백 실행 (보통 SceneManager.LoadScene).
            onComplete?.Invoke();
        }

        // fader의 알파와 입력 차단 상태를 한 번에 설정.
        private void SetFaderState(float alpha, bool blockRaycasts)
        {
            if (fader == null) return;
            fader.alpha = alpha;
            fader.blocksRaycasts = blockRaycasts;
            fader.interactable = blockRaycasts;
        }
    }
}
