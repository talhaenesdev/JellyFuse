using UnityEngine;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// Scales a popup authored at its art's size so it fills the width of a reference rect, less the
    /// margins, and takes no more than a share of its height. The popup is re-fitted whenever the
    /// reference changes size.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class PopupFitter : MonoBehaviour
    {
        [Tooltip("The rect the popup fills. Empty uses the popup's parent.")]
        [SerializeField] private RectTransform _reference;
        [Tooltip("Space kept at the popup's left and right, in the reference's units.")]
        [SerializeField, Min(0)] private float _horizontalMargin = 60;
        [Tooltip("The most of the reference's height the popup may take.")]
        [SerializeField, Range(.2f, 1)] private float _maxHeightShare = .8f;
        [Tooltip("Panel art that reaches past the popup's rect. While it shows, the popup is fitted by both together, so the art stays on screen.")]
        [SerializeField] private RectTransform _frame;

        private Vector2 _fittedFor;
        private bool _fittedWithFrame;
        private readonly Vector3[] _corners = new Vector3[4];

        private bool FrameShown => _frame != null && _frame.gameObject.activeSelf;

        private void OnEnable() => Fit();
        private void LateUpdate()
        {
            RectTransform reference = Reference;
            if (reference != null && (reference.rect.size != _fittedFor || FrameShown != _fittedWithFrame)) Fit();
        }

        private RectTransform Reference => _reference != null ? _reference : transform.parent as RectTransform;

        public void Fit()
        {
            RectTransform reference = Reference;
            var self = (RectTransform)transform;
            if (reference == null) return;
            Vector2 size = reference.rect.size;
            float width = size.x - _horizontalMargin * 2, height = size.y * _maxHeightShare;
            if (width <= 0 || height <= 0 || self.rect.width <= 0 || self.rect.height <= 0) return;
            _fittedFor = size;
            _fittedWithFrame = FrameShown;
            Rect bounds = Bounds(self);
            self.localScale = Vector3.one * Mathf.Min(width / bounds.width, height / bounds.height);
        }

        /// <summary>The popup's rect, grown by the frame's where the frame shows, in the popup's own units.</summary>
        private Rect Bounds(RectTransform self)
        {
            Rect rect = self.rect;
            if (!FrameShown) return rect;
            _frame.GetWorldCorners(_corners);
            Vector2 min = rect.min, max = rect.max;
            foreach (Vector3 corner in _corners)
            {
                Vector2 local = self.InverseTransformPoint(corner);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            // Scaled about its centred pivot, so across, the side reaching furthest decides; a crest above
            // only makes the popup taller, which the height share already leaves room for.
            float pivotX = rect.xMin + rect.width * self.pivot.x;
            float half = Mathf.Max(pivotX - min.x, max.x - pivotX);
            return new Rect(0, 0, half * 2, max.y - min.y);
        }
    }
}
