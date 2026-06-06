using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Guskapaska.Util;

namespace Guskapaska.Tutorial
{
    /// <summary>
    /// 튜토리얼 오버레이의 화면 표현을 담당하는 뷰.
    /// 흐름 제어는 TutorialController가 맡고, 이 컴포넌트는 표시만 책임진다.
    /// 비활성 시작 함정(02_Unity6_Guidelines.md §17)을 피하기 위해 GameObject는 항상 활성으로
    /// 두고 CanvasGroup 알파로 표시 여부를 제어한다.
    /// </summary>
    public class TutorialOverlayView : MonoBehaviour
    {
        [Header("Root")]
        // 오버레이 전체의 알파/입력 차단을 제어하는 CanvasGroup.
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Dim & Highlight")]
        // 화면 전체를 덮는 반투명 딤 이미지. raycastTarget을 켜 두어 뒤쪽 클릭을 막는다.
        [SerializeField] private Image dimImage;

        // 강조 대상 위에 표시되는 테두리 박스. 대상이 없으면 숨긴다.
        [SerializeField] private RectTransform highlightBox;

        [Header("Guide Panel")]
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;

        [Header("Animation")]
        // 페이드 인/아웃 지속 시간(초). Inspector에서 튜닝 가능.
        [SerializeField] private float fadeDuration = 0.3f;

        /// <summary>"다음" 버튼 클릭 시 발생.</summary>
        public event Action OnNextClicked;

        /// <summary>"건너뛰기" 버튼 클릭 시 발생.</summary>
        public event Action OnSkipClicked;

        private void Awake()
        {
            // 버튼 리스너 연결.
            if (nextButton != null) nextButton.onClick.AddListener(() => OnNextClicked?.Invoke());
            if (skipButton != null) skipButton.onClick.AddListener(() => OnSkipClicked?.Invoke());

            // 시작 시 숨김 상태 (알파 0, 입력 차단 해제).
            ApplyState(0f, false);
        }

        private void OnDisable()
        {
            // 진행 중인 페이드 코루틴 정리.
            TweenRunner.CancelAll(this);
        }

        /// <summary>오버레이를 페이드 인 하여 표시한다.</summary>
        public void FadeIn()
        {
            if (canvasGroup == null) return;

            // 표시 동안에는 딤이 뒤쪽 입력을 막아야 한다.
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            TweenRunner.Run(this, "fade",
                TweenRunner.FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 1f, fadeDuration, EasingCurves.EaseOutQuad));
        }

        /// <summary>오버레이를 페이드 아웃 하여 숨기고, 끝나면 입력 차단을 해제한다.</summary>
        public void FadeOut(Action onComplete = null)
        {
            if (canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            // 페이드 도중 버튼이 다시 눌리지 않도록 상호작용을 먼저 끈다.
            canvasGroup.interactable = false;
            TweenRunner.Run(this, "fade", FadeOutRoutine(onComplete));
        }

        // 페이드 아웃 후 딤의 raycast 차단을 해제한다.
        private IEnumerator FadeOutRoutine(Action onComplete)
        {
            yield return TweenRunner.FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 0f, fadeDuration, EasingCurves.EaseInQuad);

            // 페이드가 끝난 뒤에는 딤이 클릭을 막지 않도록 한다 (Stage 6 fader 함정과 동일).
            canvasGroup.blocksRaycasts = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 한 단계의 내용을 표시한다.
        /// </summary>
        /// <param name="body">안내 본문 텍스트.</param>
        /// <param name="highlightTarget">강조할 대상 (null이면 하이라이트 박스 숨김).</param>
        /// <param name="showNextButton">"다음" 버튼 표시 여부 (이벤트로 진행하는 단계는 숨김).</param>
        public void ShowStep(string body, RectTransform highlightTarget, bool showNextButton)
        {
            if (bodyText != null) bodyText.text = body;
            if (nextButton != null) nextButton.gameObject.SetActive(showNextButton);

            UpdateHighlight(highlightTarget);
        }

        // 강조 박스를 대상의 위치/크기에 맞춘다.
        // 회전된 카드도 감싸도록 월드 코너 기준의 bounding box로 계산한다.
        private void UpdateHighlight(RectTransform target)
        {
            if (highlightBox == null) return;

            if (target == null)
            {
                // 강조 대상이 없으면 박스를 숨긴다.
                highlightBox.gameObject.SetActive(false);
                return;
            }

            highlightBox.gameObject.SetActive(true);

            // 대상의 월드 코너 4개로 화면상 bounding box를 만든다.
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);

            Vector3 min = corners[0];
            Vector3 max = corners[0];
            for (int i = 1; i < 4; i++)
            {
                min = Vector3.Min(min, corners[i]);
                max = Vector3.Max(max, corners[i]);
            }

            // highlightBox와 대상이 같은 Screen Space - Overlay 캔버스에 있다고 가정.
            // 이 경우 월드 좌표 = 스크린 픽셀이므로 position에 그대로 대입할 수 있다.
            Vector3 center = (min + max) * 0.5f;
            highlightBox.position = center;

            // sizeDelta는 로컬 단위이므로 캔버스 스케일(lossyScale)로 보정한다.
            float scale = highlightBox.lossyScale.x;
            if (Mathf.Approximately(scale, 0f)) scale = 1f;
            highlightBox.sizeDelta = new Vector2((max.x - min.x) / scale, (max.y - min.y) / scale);
        }

        // 알파와 입력 차단 상태를 즉시 적용 (애니메이션 없이).
        private void ApplyState(float alpha, bool blocks)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = blocks;
            canvasGroup.interactable = blocks;
        }
    }
}