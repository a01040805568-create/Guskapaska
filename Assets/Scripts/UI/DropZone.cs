using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Guskapaska.UI
{
    /// <summary>
    /// A UGUI drop target that detects when a draggable
    /// <see cref="CardInteractable"/> is released over it.
    /// Attach to any RectTransform with a Raycast-enabled Graphic
    /// (e.g. the PlayerSlot Image) to receive drop events.
    /// </summary>
    public class DropZone : MonoBehaviour, IDropHandler
    {
        /// <summary>
        /// Raised when a <see cref="CardInteractable"/> is dropped over this zone.
        /// Subscribers (typically <see cref="DragController"/>) decide what to do
        /// with the dropped card.
        /// </summary>
        public event Action<CardInteractable> OnCardDropped;

        /// <inheritdoc/>
        public void OnDrop(PointerEventData eventData)
        {
            // 드래그 객체가 없으면 처리할 게 없음.
            if (eventData == null || eventData.pointerDrag == null)
            {
                return;
            }

            // 드롭된 GameObject가 CardInteractable이 아니면 무시.
            // (다른 UI 요소가 우연히 같은 raycast 경로로 들어올 가능성 대비)
            CardInteractable card = eventData.pointerDrag.GetComponent<CardInteractable>();
            if (card == null)
            {
                return;
            }

            OnCardDropped?.Invoke(card);
        }
    }
}
