using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The part of a board cat a finger can reach: it reports where it was pressed and held, in its own rect's
    /// height, and when it was let go. The press and the release are passed on to the board under it, and the drag
    /// goes to the board as well - it handles no drag of its own - so a finger that starts on a cat still pans a
    /// map that slides. What the cat does about the touch is the board view's.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CatTouchArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>Pressed, at a point from 0 to 1 up the area.</summary>
        public Action<float> Pressed;
        /// <summary>Still held, at a point from 0 to 1 up the area (above 1 once the finger has risen past it).</summary>
        public Action<float> Held;
        public Action Released;

        private RectTransform _rect;
        private PointerEventData _press;

        private void Awake() => _rect = (RectTransform)transform;

        public void OnPointerDown(PointerEventData data)
        {
            _press = data;
            Pressed?.Invoke(Height(data));
            // The board under the cat hears the press too: it decides between a jelly, a booster's target and a pan.
            if (transform.parent != null) ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, data, ExecuteEvents.pointerDownHandler);
        }

        public void OnPointerUp(PointerEventData data)
        {
            _press = null;
            Released?.Invoke();
            if (transform.parent != null) ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, data, ExecuteEvents.pointerUpHandler);
        }

        private void Update()
        {
            if (_press != null) Held?.Invoke(Height(_press));
        }

        private void OnDisable()
        {
            if (_press == null) return;
            _press = null;
            Released?.Invoke();
        }

        private float Height(PointerEventData data)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, data.position, data.pressEventCamera, out var local)) return 0;
            var rect = _rect.rect;
            return rect.height > 0 ? (local.y - rect.yMin) / rect.height : 0;
        }
    }
}
