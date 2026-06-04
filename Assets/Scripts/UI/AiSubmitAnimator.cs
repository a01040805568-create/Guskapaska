using System.Collections;
using UnityEngine;
using Guskapaska.Core;
using Guskapaska.Util;

namespace Guskapaska.UI
{
    /// <summary>
    /// Spawns a temporary CardView that flies in a parabolic arc from the AI hand
    /// to the AI submission slot. The flying card is destroyed when it arrives;
    /// the final visual is handed off to <see cref="SubmissionZoneView.ShowAiCard"/>.
    /// </summary>
    public class AiSubmitAnimator : MonoBehaviour
    {
        [Header("Anchors")]
        [Tooltip("비행 시작 지점. AI 손패의 중앙 위치를 가리키는 RectTransform.")]
        [SerializeField] private RectTransform aiHandAnchor;

        [Tooltip("비행 도착 지점. SubmissionZone의 AiSlot 위치를 가리키는 RectTransform.")]
        [SerializeField] private RectTransform aiSlotAnchor;

        [Header("Flying Card")]
        [Tooltip("비행 중에만 표시되는 임시 카드 프리팹. CardView 컴포넌트가 포함되어야 한다.")]
        [SerializeField] private GameObject flyingCardPrefab;

        [Tooltip("비행 카드가 잠시 살 부모 Transform. 보통 메인 Canvas의 RectTransform.")]
        [SerializeField] private RectTransform flyContainer;

        [Header("Animation Settings")]
        [Tooltip("포물선의 최고점 높이 (픽셀).")]
        [SerializeField] private float arcHeight = 100f;

        [Tooltip("비행 지속 시간 (초).")]
        [SerializeField] private float duration = 0.6f;

        [Tooltip("도착 직전에 카드가 살짝 커지는 효과의 최종 스케일 (1.0이면 효과 없음).")]
        [SerializeField] private float arrivalScale = 1.0f;

        private void OnDisable()
        {
            // 진행 중인 트윈/코루틴 정리. 단 이 컴포넌트는 트윈을 직접 돌리지 않고
            // 임시 GameObject들에 위임하므로, 여기서 청소할 트윈이 거의 없다.
            TweenRunner.CancelAll(this);
        }

        /// <summary>
        /// Animate an AI submission by spawning a temporary face-down card at the
        /// AI hand anchor and arcing it to the AI slot anchor. On arrival, the
        /// temporary GameObject is destroyed and <paramref name="onArrived"/> is invoked
        /// so the caller can commit the real submission visual.
        /// </summary>
        public IEnumerator AnimateAiSubmit(Card card, System.Action onArrived)
        {
            // 필수 참조 누락 시 안전한 폴백: 즉시 onArrived 호출 후 종료.
            if (flyingCardPrefab == null || aiHandAnchor == null || aiSlotAnchor == null || flyContainer == null)
            {
                Debug.LogWarning("[AiSubmitAnimator] 필수 참조가 누락되어 즉시 도착으로 폴백합니다.");
                onArrived?.Invoke();
                yield break;
            }

            // 1) 임시 비행 카드 인스턴스 생성.
            //    flyContainer를 부모로 사용 — 메인 Canvas의 RectTransform이라야
            //    UI 좌표계에서 정상적으로 그려진다.
            GameObject flyingGo = Instantiate(flyingCardPrefab, flyContainer);
            RectTransform flyingRt = flyingGo.GetComponent<RectTransform>();
            if (flyingRt == null)
            {
                // 안전망: 잘못된 프리팹이 들어왔다면 즉시 정리하고 종료.
                Debug.LogWarning("[AiSubmitAnimator] flyingCardPrefab에 RectTransform이 없습니다.");
                Destroy(flyingGo);
                onArrived?.Invoke();
                yield break;
            }

            // 2) 비행 카드는 face-down 상태로 시작. 도착 후 실제 카드는 SubmissionZoneView가
            //    face-up으로 표시한다. CardView가 부착되어 있다면 SetFaceUp(false) 호출.
            CardView flyingView = flyingGo.GetComponent<CardView>();
            if (flyingView != null)
            {
                flyingView.SetFaceUp(false);
            }

            // 3) 시작 위치 / 도착 위치를 월드 좌표로 가져온다.
            //    Canvas root 기준 localPosition으로 다루는 게 안전하므로 좌표 변환.
            Vector3 startWorld = aiHandAnchor.position;
            Vector3 endWorld = aiSlotAnchor.position;

            // flyContainer 기준 로컬 좌표로 변환 (Vector3 인자는 월드 좌표 기준).
            Vector3 startLocal = flyContainer.InverseTransformPoint(startWorld);
            Vector3 endLocal = flyContainer.InverseTransformPoint(endWorld);

            // 시작 위치로 즉시 배치.
            flyingRt.localPosition = startLocal;
            flyingRt.localScale = Vector3.one;
            flyingRt.localRotation = Quaternion.identity;

            // 4) 포물선 비행. TweenRunner.MoveLocalArc 활용.
            //    트윈 호스트는 임시 GameObject가 아니라 이 AiSubmitAnimator 자신.
            //    이유: 임시 GameObject가 트윈 도중 파괴되면 코루틴이 끊어진다.
            //    여기서는 직접 코루틴을 돌려 매 프레임 위치를 보간한다.

            float elapsed = 0f;
            AnimationCurve curve = EasingCurves.EaseInOutQuad;
            Vector3 startScale = flyingRt.localScale;
            Vector3 endScale = Vector3.one * arrivalScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float k = curve.Evaluate(u);

                // 위치는 곡선 적용한 k로 직선 보간 후, u 기반 포물선 오프셋을 더한다.
                Vector3 linearPos = Vector3.LerpUnclamped(startLocal, endLocal, k);
                float arcOffset = 4f * arcHeight * u * (1f - u);
                flyingRt.localPosition = linearPos + new Vector3(0f, arcOffset, 0f);

                // 스케일도 함께 보간 (도착 시 커지는 효과 옵션).
                flyingRt.localScale = Vector3.LerpUnclamped(startScale, endScale, k);

                // 도중에 임시 GameObject가 파괴되면 안전하게 종료.
                if (flyingRt == null)
                {
                    onArrived?.Invoke();
                    yield break;
                }

                yield return null;
            }

            // 정확한 도착 위치 보장 (부동소수점 오차 방지).
            if (flyingRt != null)
            {
                flyingRt.localPosition = endLocal;
                flyingRt.localScale = endScale;
            }

            // 5) 실제 카드 시각을 SubmissionZoneView에 넘긴다.
            //    callback이 ShowAiCard를 호출할 책임을 진다.
            onArrived?.Invoke();

            // 6) 임시 비행 GameObject 정리.
            if (flyingGo != null)
            {
                Destroy(flyingGo);
            }
        }
    }
}
