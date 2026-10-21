using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// A text background built from two pictures: a left cap, and a middle piece. The cap is drawn on
    /// the left as painted and mirrored on the right; the middle fills the space between, as long as
    /// the label needs. With a ContentSizeFitter (Horizontal: Preferred Size) beside it, the
    /// background grows and shrinks with its text.
    ///
    /// It keeps two child graphics of its own - Img_Caps (both ends) and Img_Middle - so the pieces
    /// may come from separate textures; packed in one atlas they draw together. The label is a child
    /// too, laid between the caps with Padding on each side.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MirroredCapBackground : UIBehaviour, ILayoutGroup, ILayoutElement
    {
        [Tooltip("The left end, as painted. The right end is this, mirrored.")]
        [SerializeField] private Sprite _leftCap;
        [Tooltip("The middle piece, stretched (or 9-sliced, when it has a border) between the ends.")]
        [SerializeField] private Sprite _middle;
        [SerializeField] private Color _colour = Color.white;
        [Tooltip("The text the background is sized to, a child of this object: a Text, a TextMeshPro text, or any graphic with a preferred width.")]
        [SerializeField] private RectTransform _label;
        [Tooltip("Space between each end and the text, in units.")]
        [SerializeField] private float _padding = 8;
        [Tooltip("How far the text may sit into the ends, in units: positive lets it start over the cap's curve.")]
        [SerializeField] private float _labelIntoCaps;
        [Tooltip("The narrowest the background gets, in units, however short the text.")]
        [SerializeField] private float _minWidth;

        [SerializeField, HideInInspector] private MirroredCapGraphic _caps;
        [SerializeField, HideInInspector] private Image _middleImage;

        private RectTransform _rect;
        private RectTransform Rect => _rect != null ? _rect : _rect = (RectTransform)transform;

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureParts();
            Apply();
            SetDirty();
        }
        protected override void OnDisable()
        {
            SetDirty();
            base.OnDisable();
        }
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (isActiveAndEnabled) { Arrange(); SetDirty(); }
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            // Creating children is not allowed while Unity validates; it is done on the next editor tick.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                EnsureParts();
                Apply();
                SetDirty();
            };
        }
#endif

        /// <summary>Changes the pictures from code, for a game that picks them at run time.</summary>
        public void SetSprites(Sprite leftCap, Sprite middle)
        {
            _leftCap = leftCap; _middle = middle;
            Apply();
            SetDirty();
        }

        // ---------- parts ----------

        private void EnsureParts()
        {
            if (_caps == null) _caps = Part("Img_Caps").gameObject.AddComponent<MirroredCapGraphic>();
            if (_middleImage == null) _middleImage = Part("Img_Middle").gameObject.AddComponent<Image>();
            // Behind the label: the middle first, the ends over its edges.
            _middleImage.transform.SetSiblingIndex(0);
            _caps.transform.SetSiblingIndex(1);
        }
        private RectTransform Part(string name)
        {
            var existing = transform.Find(name);
            if (existing != null) Destroy(existing.gameObject);
            var part = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            part.gameObject.layer = gameObject.layer;
            part.SetParent(transform, false);
            return part;
        }
        private static void Destroy(GameObject target)
        {
            if (Application.isPlaying) Object.Destroy(target); else Object.DestroyImmediate(target);
        }

        private void Apply()
        {
            if (_caps == null || _middleImage == null) return;
            _caps.SetCap(_leftCap);
            _caps.color = _colour;
            _caps.raycastTarget = false;
            _middleImage.sprite = _middle;
            _middleImage.type = _middle != null && _middle.border.sqrMagnitude > 0 ? Image.Type.Sliced : Image.Type.Simple;
            _middleImage.color = _colour;
            _middleImage.raycastTarget = false;
            _middleImage.enabled = _middle != null;
            Arrange();
        }

        /// <summary>The ends fill the whole rect; the middle and the label sit between them.</summary>
        private void Arrange()
        {
            if (_caps == null || _middleImage == null) return;
            float cap = CapWidth;
            Stretch((RectTransform)_caps.transform, 0, 0);
            // The middle tucks half a unit under each end, so no seam opens between them.
            Stretch((RectTransform)_middleImage.transform, cap - .5f, cap - .5f);
            if (_label != null) Stretch(_label, cap + _padding - _labelIntoCaps, cap + _padding - _labelIntoCaps);
        }
        private static void Stretch(RectTransform part, float left, float right)
        {
            part.anchorMin = Vector2.zero; part.anchorMax = Vector2.one; part.pivot = new Vector2(.5f, .5f);
            part.offsetMin = new Vector2(left, 0); part.offsetMax = new Vector2(-right, 0);
        }

        private float CapWidth => _caps != null ? _caps.CapWidth(Rect.rect.height) : 0;
        private float LabelWidth => _label != null && _label.TryGetComponent(out ILayoutElement text) ? text.preferredWidth : 0;

        private void SetDirty()
        {
            if (!IsActive()) return;
            LayoutRebuilder.MarkLayoutForRebuild(Rect);
        }

        // ---------- layout: the label is a child, so a change of text reaches this object ----------

        public void SetLayoutHorizontal() => Arrange();
        public void SetLayoutVertical() { }

        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public float minWidth => _minWidth;
        public float preferredWidth => Mathf.Max(_minWidth, CapWidth * 2 + (_padding - _labelIntoCaps) * 2 + LabelWidth);
        public float flexibleWidth => -1;
        public float minHeight => -1;
        public float preferredHeight => -1;
        public float flexibleHeight => -1;
        public int layoutPriority => 1;
    }
}
