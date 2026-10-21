using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    public sealed class JellyDragSurface : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IInitializePotentialDragHandler
    {
        private RectTransform _board;
        private Action<Vector2> _pressed, _moved, _released;
        private Action _cancelled;
        private int? _pointer;
        public void Initialize(RectTransform board, Action<Vector2> pressed, Action<Vector2> moved, Action<Vector2> released, Action cancelled)
        { _board = board; _pressed = pressed; _moved = moved; _released = released; _cancelled = cancelled; }
        public void ResetPointer() => _pointer = null;
        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;
        private Vector2 Local(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_board, data.position, data.pressEventCamera, out var point);
            return point;
        }
        public void OnPointerDown(PointerEventData data)
        {
            if (_pointer.HasValue || data.button != PointerEventData.InputButton.Left) return;
            _pointer = data.pointerId; _pressed?.Invoke(Local(data));
        }
        public void OnDrag(PointerEventData data) { if (_pointer == data.pointerId) _moved?.Invoke(Local(data)); }
        public void OnPointerUp(PointerEventData data)
        {
            if (_pointer != data.pointerId) return;
            _pointer = null; _released?.Invoke(Local(data));
        }
        private void Cancel() { if (!_pointer.HasValue) return; _pointer = null; _cancelled?.Invoke(); }
        private void OnDisable() => Cancel();
        private void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
        private void OnApplicationPause(bool paused) { if (paused) Cancel(); }
    }
}
