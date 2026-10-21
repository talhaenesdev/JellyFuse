using System;
using System.Collections.Generic;
using Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// A tutorial's dark veil over the whole screen, with holes over the cells the player may use, and a
    /// hand that shows the move: it presses on the jelly, slides it along the way and lets go at the end.
    /// A touch on the veil goes nowhere; a touch in a hole goes through to the board.
    /// </summary>
    internal sealed class TutorialVeil : MaskableGraphic, ICanvasRaycastFilter
    {
        private JellySpriteBoardView _board;
        private CD_JellyBoardLook _look;
        private Func<bool> _holding;
        private Image _hand;
        private readonly List<Vector2Int> _lit = new();
        private readonly List<Vector2> _handPath = new();
        private readonly List<Vector2Int> _target = new();
        private readonly List<Image> _marks = new();
        private Color _targetColour;
        private readonly List<Rect> _holes = new();
        private readonly List<Rect> _measured = new();
        private readonly List<Vector2> _points = new();
        private float _time;
        // A booster's focus instead of a tutorial: the squares to light, read each frame, and screen parts kept clear.
        private Action<List<(Vector3 Min, Vector3 Max)>> _focusAreas;
        private readonly List<(Vector3 Min, Vector3 Max)> _areas = new();
        private readonly List<RectTransform> _open = new();
        private readonly Vector3[] _corners = new Vector3[4];
        // Whether the dark has been drawn for what is shown now; with no hole at all it still has to be drawn once.
        private bool _drawn;

        public static TutorialVeil Create(RectTransform parent)
        {
            var go = new GameObject("TutorialVeil", typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var veil = go.AddComponent<TutorialVeil>();
            veil.raycastTarget = true;
            veil._hand = new GameObject("Img_TutorialHand", typeof(RectTransform)).AddComponent<Image>();
            veil._hand.gameObject.layer = go.layer;
            veil._hand.rectTransform.SetParent(rect, false);
            veil._hand.raycastTarget = false;
            veil._hand.preserveAspect = true;
            veil._text = new GameObject("Txt_Tutorial", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            veil._text.gameObject.layer = go.layer;
            veil._text.rectTransform.SetParent(rect, false);
            veil._text.raycastTarget = false;
            veil._text.alignment = TextAlignmentOptions.Center;
            veil._text.enableWordWrapping = true;
            go.SetActive(false);
            return veil;
        }

        private TextMeshProUGUI _text;
        /// <summary>The step's text, in the language in use; empty hides it.</summary>
        public void ShowText(string text)
        {
            if (_look == null) return;
            _text.gameObject.SetActive(!string.IsNullOrEmpty(text));
            _text.text = text ?? "";
            if (_look.TutorialTextFont != null) _text.font = _look.TutorialTextFont;
            _text.fontSize = _look.TutorialTextSize;
            _text.color = _look.TutorialTextColour;
            _text.outlineColor = _look.TutorialTextOutline;
            _text.outlineWidth = _look.TutorialTextOutline.a > 0 ? _look.TutorialTextOutlineWidth : 0;
            PlaceText();
        }
        /// <summary>The text where the look puts it on the screen: read again every frame, so a change shows at once.</summary>
        private void PlaceText()
        {
            if (_look == null || !_text.gameObject.activeSelf) return;
            var area = rectTransform.rect;
            _text.rectTransform.anchorMin = _text.rectTransform.anchorMax = new Vector2(.5f, .5f);
            _text.rectTransform.pivot = new Vector2(.5f, .5f);
            _text.rectTransform.sizeDelta = new Vector2(area.width * _look.TutorialTextWidth, _look.TutorialTextSize * 4);
            _text.rectTransform.anchoredPosition = area.min + Vector2.Scale(area.size, _look.TutorialTextAnchor) - area.center + _look.TutorialTextOffset;
            _text.transform.SetAsLastSibling();
        }
        public void Show(JellySpriteBoardView board, CD_JellyBoardLook look, JellyTutorialRVO tutorial, Func<bool> holding)
        {
            _board = board; _look = look; _holding = holding;
            _focusAreas = null; _open.Clear(); _drawn = false;
            _lit.Clear(); _lit.AddRange(tutorial.Lit);
            _handPath.Clear(); _handPath.AddRange(tutorial.HandPath);
            _target.Clear(); _target.AddRange(tutorial.Target);
            _targetColour = board.JellyTint(tutorial.Colour);
            color = new Color(0, 0, 0, look.TutorialDim);
            _hand.sprite = look.TutorialHandSprite;
            // The fingertip of the picture is where it touches.
            _hand.rectTransform.pivot = look.TutorialHandTip;
            _time = 0;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            CoverCanvas();
            _holes.Clear();
            SetVerticesDirty();
        }
        /// <summary>
        /// The same veil while a booster waits for its target: light over what it may take (<paramref name="areas"/>,
        /// read each frame) and over <paramref name="open"/> - the booster row and its hint - with no hand and no text.
        /// </summary>
        public void ShowFocus(JellySpriteBoardView board, CD_JellyBoardLook look, Action<List<(Vector3 Min, Vector3 Max)>> areas, params RectTransform[] open)
        {
            _board = board; _look = look; _holding = null; _focusAreas = areas; _drawn = false;
            _lit.Clear(); _handPath.Clear(); _target.Clear();
            _open.Clear(); foreach (var rect in open) if (rect != null) _open.Add(rect);
            ShowText(null);
            color = new Color(0, 0, 0, look.TutorialDim);
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            CoverCanvas();
            _holes.Clear();
            SetVerticesDirty();
        }
        /// <summary>Stretched over the whole canvas: the screen it sits in may stop short of the edges, inside the safe area.</summary>
        private void CoverCanvas()
        {
            var root = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            var parent = rectTransform.parent as RectTransform;
            if (root == null || parent == null) return;
            var corners = new Vector3[4];
            root.GetWorldCorners(corners);
            Vector2 min = parent.InverseTransformPoint(corners[0]), max = parent.InverseTransformPoint(corners[2]);
            var area = parent.rect;
            rectTransform.offsetMin = min - area.min;
            rectTransform.offsetMax = max - area.max;
        }
        public void Hide() { if (gameObject.activeSelf) gameObject.SetActive(false); }

        private void LateUpdate()
        {
            if (_look == null) return;
            Measure();
            MarkTarget();
            if (Application.isEditor) PlaceText();
            AnimateHand(Time.unscaledDeltaTime);
        }
        /// <summary>The holes follow the board: it may be laid out again, or still be settling, after the tutorial shows.</summary>
        private void Measure()
        {
            _measured.Clear();
            if (_focusAreas != null && _board != null)
            {
                if (_board.CellCorners(Vector2Int.zero, out var c0, out var c1))
                    _cellSize = Mathf.Abs(rectTransform.InverseTransformPoint(c1).x - rectTransform.InverseTransformPoint(c0).x);
                _areas.Clear(); _focusAreas(_areas);
                float pad = _cellSize * _look.TutorialHolePadding;
                foreach (var area in _areas)
                {
                    Vector2 a = rectTransform.InverseTransformPoint(area.Min), b = rectTransform.InverseTransformPoint(area.Max);
                    _measured.Add(Rect.MinMaxRect(Mathf.Min(a.x, b.x) - pad, Mathf.Min(a.y, b.y) - pad, Mathf.Max(a.x, b.x) + pad, Mathf.Max(a.y, b.y) + pad + _cellSize * _look.TutorialHoleLift));
                }
            }
            foreach (var open in _open)
            {
                if (open == null || !open.gameObject.activeInHierarchy) continue;
                open.GetWorldCorners(_corners);
                Vector2 a = rectTransform.InverseTransformPoint(_corners[0]), b = rectTransform.InverseTransformPoint(_corners[2]);
                _measured.Add(Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)));
            }
            foreach (var cell in _lit)
            {
                if (_board == null || !_board.CellCorners(cell, out var min, out var max)) continue;
                Vector2 a = rectTransform.InverseTransformPoint(min), b = rectTransform.InverseTransformPoint(max);
                var r = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                // A little round each cell, and more above it, where a lifted jelly rises.
                _cellSize = r.width;
                float pad = r.width * _look.TutorialHolePadding;
                _measured.Add(Rect.MinMaxRect(r.xMin - pad, r.yMin - pad, r.xMax + pad, r.yMax + pad + r.height * _look.TutorialHoleLift));
            }
            bool same = _measured.Count == _holes.Count;
            for (int i = 0; same && i < _holes.Count; i++) same = Close(_measured[i], _holes[i]);
            if (same && _drawn) return;
            _drawn = true;
            _holes.Clear(); _holes.AddRange(_measured);
            DrawMask();
            SetMaterialDirty();
            SetVerticesDirty();
        }
        private static bool Close(Rect a, Rect b) => (a.min - b.min).sqrMagnitude < .01f && (a.max - b.max).sqrMagnitude < .01f;

        /// <summary>The cells the jelly is to end on, in its colour, pulsing under the hand.</summary>
        private void MarkTarget()
        {
            int used = 0;
            if (_board == null) return;
            float pulse = .6f + .4f * Mathf.Cos(Time.unscaledTime * _look.TutorialTargetPulse * Mathf.PI * 2);
            foreach (var cell in _target)
            {
                if (!_board.CellCorners(cell, out var min, out var max)) break;
                Vector2 a = rectTransform.InverseTransformPoint(min), b = rectTransform.InverseTransformPoint(max);
                if (used == _marks.Count)
                {
                    var mark = new GameObject("Img_TutorialTarget", typeof(RectTransform)).AddComponent<Image>();
                    mark.gameObject.layer = gameObject.layer;
                    mark.rectTransform.SetParent(rectTransform, false);
                    mark.raycastTarget = false;
                    _marks.Add(mark);
                }
                var image = _marks[used++];
                image.gameObject.SetActive(true);
                image.sprite = _board.RoundedSprite;
                image.type = Image.Type.Sliced;
                image.rectTransform.anchoredPosition = (a + b) * .5f - rectTransform.rect.center;
                image.rectTransform.sizeDelta = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y)) * .86f;
                var colour = _targetColour; colour.a = _look.TutorialTargetAlpha * pulse;
                image.color = colour;
            }
            for (int i = used; i < _marks.Count; i++) _marks[i].gameObject.SetActive(false);
            // Under the hand.
            _hand.transform.SetAsLastSibling();
        }
        private void AnimateHand(float dt)
        {
            if (_board == null || _hand.sprite == null || _handPath.Count < 2 || (_holding != null && _holding()))
            {
                // While the player holds the jelly the hand steps aside, and starts over once they let go.
                _hand.enabled = false; _time = 0; return;
            }
            _points.Clear();
            float cellSize = 0;
            foreach (var cell in _handPath)
            {
                if (!_board.CellCorners(cell, out var min, out var max)) return;
                Vector2 a = rectTransform.InverseTransformPoint(min), b = rectTransform.InverseTransformPoint(max);
                _points.Add((a + b) * .5f);
                cellSize = Mathf.Abs(b.x - a.x);
            }
            float length = 0;
            for (int i = 1; i < _points.Count; i++) length += Vector2.Distance(_points[i - 1], _points[i]);
            float appear = .3f, press = .25f, move = length / Mathf.Max(1, cellSize * _look.TutorialHandSpeed), release = .25f, fade = .3f, wait = .45f;
            float cycle = appear + press + move + release + fade + wait;
            _time = (_time + dt) % cycle;
            float t = _time, alpha = 1, scale = 1, along = 0;
            if (t < appear) { alpha = t / appear; scale = Mathf.Lerp(1.15f, 1, alpha); }
            else if ((t -= appear) < press) scale = Mathf.Lerp(1, .85f, t / press);
            else if ((t -= press) < move) { scale = .85f; along = Smooth(t / move); }
            else if ((t -= move) < release) { along = 1; scale = Mathf.Lerp(.85f, 1, t / release); }
            else if ((t -= release) < fade) { along = 1; alpha = 1 - t / fade; }
            else { along = 1; alpha = 0; }
            _hand.enabled = true;
            _hand.rectTransform.anchoredPosition = PointAlong(along * length);
            _hand.rectTransform.sizeDelta = Vector2.one * cellSize * _look.TutorialHandSize;
            _hand.rectTransform.localScale = Vector3.one * scale;
            _hand.color = new Color(1, 1, 1, alpha);
        }
        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
        private Vector2 PointAlong(float distance)
        {
            // Points are in the veil's local space, whose centre the hand's anchors sit on.
            var offset = rectTransform.rect.center;
            for (int i = 1; i < _points.Count; i++)
            {
                float step = Vector2.Distance(_points[i - 1], _points[i]);
                if (distance <= step) return Vector2.Lerp(_points[i - 1], _points[i], step > 0 ? distance / step : 1) - offset;
                distance -= step;
            }
            return _points[_points.Count - 1] - offset;
        }

        /// <summary>
        /// The dark is a texture over the whole veil: clear in the holes, fading to Tutorial Dim over Tutorial
        /// Edge Softness out from them, so the light has soft, rounded edges. Drawn again only when a hole moves.
        /// </summary>
        private Texture2D _mask;
        private float _cellSize = 100;
        // Texture pixels per veil unit: fine enough for a soft edge, which the texture's filtering smooths further.
        private const float MASK_DENSITY = .2f;
        public override Texture mainTexture => _mask != null ? _mask : s_WhiteTexture;
        private void DrawMask()
        {
            var r = rectTransform.rect;
            int width = Mathf.Clamp(Mathf.CeilToInt(r.width * MASK_DENSITY), 4, 1024), height = Mathf.Clamp(Mathf.CeilToInt(r.height * MASK_DENSITY), 4, 1024);
            if (_mask == null || _mask.width != width || _mask.height != height)
            {
                if (_mask != null) Destroy(_mask);
                _mask = new Texture2D(width, height, TextureFormat.Alpha8, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "TutorialVeilMask" };
            }
            float soft = Mathf.Max(.001f, _cellSize * _look.TutorialEdgeSoftness), dim = _look.TutorialDim;
            var pixels = new byte[width * height];
            for (int y = 0; y < height; y++)
            {
                float py = r.yMin + (y + .5f) / height * r.height;
                for (int x = 0; x < width; x++)
                {
                    float px = r.xMin + (x + .5f) / width * r.width;
                    // How far the pixel is from the nearest light, 0 inside one.
                    float nearest = float.MaxValue;
                    foreach (var hole in _holes)
                    {
                        float dx = Mathf.Max(hole.xMin - px, 0, px - hole.xMax), dy = Mathf.Max(hole.yMin - py, 0, py - hole.yMax);
                        nearest = Mathf.Min(nearest, dx * dx + dy * dy);
                        if (nearest <= 0) break;
                    }
                    float t = _look.TutorialEdgeSoftness <= 0 ? (nearest > 0 ? 1 : 0) : Mathf.Clamp01(Mathf.Sqrt(nearest) / soft);
                    pixels[y * width + x] = (byte)Mathf.RoundToInt(dim * t * t * (3 - 2 * t) * 255);
                }
            }
            _mask.SetPixelData(pixels, 0);
            _mask.Apply(false);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            // Black, its alpha from the mask.
            var colour = new Color32(0, 0, 0, 255);
            vh.AddVert(new Vector3(r.xMin, r.yMin), colour, new Vector2(0, 0));
            vh.AddVert(new Vector3(r.xMin, r.yMax), colour, new Vector2(0, 1));
            vh.AddVert(new Vector3(r.xMax, r.yMax), colour, new Vector2(1, 1));
            vh.AddVert(new Vector3(r.xMax, r.yMin), colour, new Vector2(1, 0));
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(2, 3, 0);
        }
        protected override void OnDestroy()
        {
            if (_mask != null) Destroy(_mask);
            base.OnDestroy();
        }
        private bool InHole(Vector2 point)
        {
            foreach (var hole in _holes) if (hole.Contains(point)) return true;
            return false;
        }
        /// <summary>Only a touch outside every hole is taken by the veil.</summary>
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local)) return true;
            return !InHole(local);
        }
    }
}
