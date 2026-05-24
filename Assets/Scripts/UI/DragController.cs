using System;
using System.Collections.Generic;
using UnityEngine;

namespace Guskapaska.UI
{
    /// <summary>
    /// Mediates between draggable <see cref="CardInteractable"/> instances and
    /// the player's <see cref="DropZone"/>. Tracks the currently active drag,
    /// fires a high-level submission event on successful drop, and instructs
    /// cards to return to their origin on a failed drop.
    /// All wiring is via Inspector references — no runtime lookups.
    /// </summary>
    public class DragController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private DropZone playerDropZone;
        [SerializeField] private HandView playerHandView;

        /// <summary>
        /// Raised when a player card has been successfully dropped onto the
        /// player drop zone. The card has not yet been removed from the hand;
        /// subscribers decide how to process the submission.
        /// </summary>
        public event Action<CardInteractable> OnPlayerCardSubmitted;

        // 현재 드래그 중인 카드. 드롭 성공 시 null로 정리되어 복귀 로직이 건너뛰어진다.
        private CardInteractable _activeDrag;

        // 이벤트를 구독한 카드들의 집합. 해제 누락과 중복 구독을 막기 위해 추적.
        private readonly HashSet<CardInteractable> _registered = new HashSet<CardInteractable>();

        private void Awake()
        {
            // Inspector 연결 누락을 조기에 알린다.
            if (playerDropZone == null)
            {
                Debug.LogError($"[DragController] {nameof(playerDropZone)} 가 연결되지 않았습니다. Inspector에서 PlayerSlot의 DropZone을 연결하세요.");
            }
            if (playerHandView == null)
            {
                Debug.LogError($"[DragController] {nameof(playerHandView)} 가 연결되지 않았습니다. Inspector에서 PlayerHandView를 연결하세요.");
            }
        }

        private void OnEnable()
        {
            if (playerDropZone != null)
            {
                playerDropZone.OnCardDropped += HandlePlayerDrop;
            }
        }

        private void OnDisable()
        {
            if (playerDropZone != null)
            {
                playerDropZone.OnCardDropped -= HandlePlayerDrop;
            }

            // 카드 이벤트도 모두 해제 — 컴포넌트가 비활성화되거나 파괴될 때
            // 카드 쪽 핸들러 참조가 남으면 메모리 누수 / 중복 호출이 발생할 수 있다.
            foreach (CardInteractable card in _registered)
            {
                if (card == null)
                {
                    continue;
                }
                card.OnDragStarted -= HandleDragStarted;
                card.OnDragEnded -= HandleDragEnded;
            }
            _registered.Clear();
            _activeDrag = null;
        }

        /// <summary>
        /// Registers drag callbacks on every CardInteractable currently displayed
        /// by the player hand view. Safe to call repeatedly — duplicate
        /// subscriptions are prevented and stale subscriptions are pruned.
        /// </summary>
        public void RegisterPlayerCards()
        {
            if (playerHandView == null)
            {
                return;
            }

            // 이번 갱신에서 살아남는 카드들을 모은다.
            HashSet<CardInteractable> currentSet = new HashSet<CardInteractable>();

            IReadOnlyList<CardView> activeViews = playerHandView.ActiveViews;
            if (activeViews != null)
            {
                for (int i = 0; i < activeViews.Count; i++)
                {
                    CardView view = activeViews[i];
                    if (view == null)
                    {
                        continue;
                    }

                    CardInteractable card = view.GetComponent<CardInteractable>();
                    if (card == null)
                    {
                        // 플레이어 손패의 CardView 프리팹에는 반드시 CardInteractable이 붙어 있어야 함.
                        Debug.LogWarning($"[DragController] {view.name} 에 CardInteractable이 없습니다. 프리팹 설정을 확인하세요.");
                        continue;
                    }

                    currentSet.Add(card);

                    // 중복 구독 방지: 이미 등록된 경우 한 번 해제 후 다시 구독 (혹시 모를 안전망).
                    // 신규 카드도 같은 흐름으로 처리되어 항상 정확히 한 번만 구독된 상태를 보장한다.
                    card.OnDragStarted -= HandleDragStarted;
                    card.OnDragEnded -= HandleDragEnded;
                    card.OnDragStarted += HandleDragStarted;
                    card.OnDragEnded += HandleDragEnded;
                }
            }

            // 더 이상 손패에 없는 카드의 구독은 해제하고 집합에서 제거.
            // (라운드 종료로 사라진 카드 인스턴스 등)
            _registered.RemoveWhere(card =>
            {
                if (card == null || !currentSet.Contains(card))
                {
                    if (card != null)
                    {
                        card.OnDragStarted -= HandleDragStarted;
                        card.OnDragEnded -= HandleDragEnded;
                    }
                    return true;
                }
                return false;
            });

            // 새로 등장한 카드들을 집합에 추가.
            foreach (CardInteractable card in currentSet)
            {
                _registered.Add(card);
            }
        }

        /// <summary>
        /// Toggles drag availability for every currently registered card.
        /// When disabling, any card mid-hover is forcibly returned to its rest
        /// state so the visual is consistent with the new state.
        /// </summary>
        public void SetAllInteractable(bool interactable)
        {
            foreach (CardInteractable card in _registered)
            {
                if (card == null)
                {
                    continue;
                }

                card.Interactable = interactable;

                // 비활성화 전환 시, 호버 상태로 남은 카드는 강제로 원상 복귀.
                if (!interactable)
                {
                    card.ForceUnhover();
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 내부 이벤트 핸들러
        // ─────────────────────────────────────────────────────────────

        private void HandleDragStarted(CardInteractable card)
        {
            _activeDrag = card;
        }

        private void HandleDragEnded(CardInteractable card, bool _)
        {
            // _activeDrag가 여전히 이 카드와 같다는 것은
            // → HandlePlayerDrop이 호출되지 않았다는 뜻 → 드롭 실패 → 복귀.
            if (_activeDrag == card)
            {
                card.ReturnToOrigin();
            }
            // _activeDrag가 null이라면 HandlePlayerDrop이 먼저 실행되어 정리한 것.
            // 이미 제출된 카드이므로 복귀시키면 안 됨.

            _activeDrag = null;
        }

        private void HandlePlayerDrop(CardInteractable card)
        {
            OnPlayerCardSubmitted?.Invoke(card);

            // 제출된 카드는 복귀 대상이 아니므로 _activeDrag를 미리 비운다.
            // (이후 호출되는 HandleDragEnded가 _activeDrag == card 체크에 걸리지 않도록 함)
            _activeDrag = null;
        }
    }
}
