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

        private Vector2 _fittedFor;

        private void OnEnable() => Fit();
        private void LateUpdate()
        {
            RectTransform reference = Reference;
            if (reference != null && reference.rect.size != _fittedFor) Fit();
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
            self.localScale = Vector3.one * Mathf.Min(width / self.rect.width, height / self.rect.height);
        }
    }
}
