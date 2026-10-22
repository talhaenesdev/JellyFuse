using UnityEngine;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// Stretches a background past the safe area to the real edges of the screen. Screens sit in
    /// layers the ScreenManager shrinks to the safe area, so buttons stay clear of a notch, camera
    /// hole or home bar - and so would a background, leaving those strips empty. Put this on
    /// backgrounds, bars and dims only: the edges ticked are pushed out to the screen's edge, the
    /// rest keep where they were authored. Re-fitted whenever the screen or the layer changes size.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenBleed : MonoBehaviour
    {
        [Tooltip("Which edges reach out to the screen's edge.")]
        [SerializeField] private bool _top = true;
        [SerializeField] private bool _bottom = true;
        [SerializeField] private bool _left = true;
        [SerializeField] private bool _right = true;
        [Tooltip("Above 0: the picture keeps this width / height and grows to cover the area, cropping what spills over. 0 stretches it.")]
        [SerializeField, Min(0)] private float _coverAspect;

        private bool _captured;
        private Vector2 _anchorMin, _anchorMax, _offsetMin, _offsetMax, _pivot;
        private Vector2 _fittedCanvas, _fittedParent;
        private readonly Vector3[] _corners = new Vector3[4];

        private RectTransform Self => (RectTransform)transform;

        private void OnEnable() => Fit();

        private void LateUpdate()
        {
            var canvas = CanvasRect();
            var parent = transform.parent as RectTransform;
            if (canvas == null || parent == null) return;
            if (canvas.rect.size != _fittedCanvas || parent.rect.size != _fittedParent) Fit();
        }

        private RectTransform CanvasRect()
        {
            var canvas = GetComponentInParent<Canvas>();
            return canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
        }

        /// <summary>
        /// Takes the rect as it stands now as the authored one and fits again. For whatever lays the
        /// object out before the bleed - a ScreenLayout - so the bleed grows from the new place.
        /// </summary>
        /// <summary>The rect as authored, before the bleed grew it; false before the first fit.</summary>
        public bool TryGetAuthored(out Vector2 anchorMin, out Vector2 anchorMax, out Vector2 pivot, out Vector2 anchoredPosition, out Vector2 sizeDelta)
        {
            anchorMin = _anchorMin; anchorMax = _anchorMax; pivot = _pivot;
            sizeDelta = _offsetMax - _offsetMin;
            anchoredPosition = _offsetMin + Vector2.Scale(sizeDelta, _pivot);
            return _captured;
        }

        public void Recapture()
        {
            _captured = false;
            Fit();
        }

        public void Fit()
        {
            var parent = transform.parent as RectTransform;
            var canvas = CanvasRect();
            if (parent == null || canvas == null) return;
            var self = Self;
            // Always start from the layout as authored, so fitting again never compounds.
            if (!_captured)
            {
                _captured = true;
                _anchorMin = self.anchorMin; _anchorMax = self.anchorMax; _offsetMin = self.offsetMin; _offsetMax = self.offsetMax; _pivot = self.pivot;
            }
            self.anchorMin = _anchorMin; self.anchorMax = _anchorMax; self.offsetMin = _offsetMin; self.offsetMax = _offsetMax; self.pivot = _pivot;

            // The authored rect and the whole screen, both in the parent's space.
            Rect parentRect = parent.rect;
            Vector2 min = parentRect.min + Vector2.Scale(_anchorMin, parentRect.size) + _offsetMin;
            Vector2 max = parentRect.min + Vector2.Scale(_anchorMax, parentRect.size) + _offsetMax;
            canvas.GetWorldCorners(_corners);
            Vector2 screenMin = parent.InverseTransformPoint(_corners[0]), screenMax = parent.InverseTransformPoint(_corners[2]);

            if (_left) min.x = Mathf.Min(min.x, screenMin.x);
            if (_bottom) min.y = Mathf.Min(min.y, screenMin.y);
            if (_right) max.x = Mathf.Max(max.x, screenMax.x);
            if (_top) max.y = Mathf.Max(max.y, screenMax.y);

            if (_coverAspect > 0)
            {
                Vector2 size = max - min, centre = (min + max) * .5f;
                if (size.x / Mathf.Max(1e-3f, size.y) > _coverAspect) size.y = size.x / _coverAspect; else size.x = size.y * _coverAspect;
                min = centre - size * .5f; max = centre + size * .5f;
            }

            // Placed by its parent's corner, so the parent's own anchoring no longer matters.
            self.anchorMin = self.anchorMax = Vector2.zero;
            self.sizeDelta = max - min;
            self.anchoredPosition = min - parentRect.min + Vector2.Scale(self.pivot, max - min);

            _fittedCanvas = canvas.rect.size;
            _fittedParent = parentRect.size;
        }
    }
}
