using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The part of a board cat a finger can reach: it reports where it was pressed, moved and let go, in its
    /// own rect's space, and nothing else. What the cat does about it is the board view's.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CatTouchArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerExitHandler
    {
        /// <summary>Pressed, at a point from 0 to 1 up the area.</summary>
        public Action<float> Pressed;
        /// <summary>Still held, at a point from 0 to 1 up the area (above 1 once the finger has risen past it).</summary>
        public Action<float> Held;
        public Action Released;

        private RectTransform _rect;

        private void Awake() => _rect = (RectTransform)transform;

        public void OnPointerDown(PointerEventData data) => Pressed?.Invoke(Height(data));
        public void OnDrag(PointerEventData data) => Held?.Invoke(Height(data));
        public void OnPointerUp(PointerEventData data) => Released?.Invoke();
        public void OnPointerExit(PointerEventData data) { if (!data.dragging) Released?.Invoke(); }

        private float Height(PointerEventData data)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, data.position, data.pressEventCamera, out var local)) return 0;
            var rect = _rect.rect;
            return rect.height > 0 ? (local.y - rect.yMin) / rect.height : 0;
        }
    }
}
