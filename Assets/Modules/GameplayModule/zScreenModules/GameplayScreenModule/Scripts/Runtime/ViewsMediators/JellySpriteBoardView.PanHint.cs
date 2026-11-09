using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// A wide level that asks for it (PanHint) opens with a hand sweeping across the board and a line saying the
    /// map slides. Both stay over the board, not on the map, and touch nothing: the player slides the map as they
    /// would anyway, and once it has moved a cell the hint fades out for this attempt.
    /// </summary>
    public partial class JellySpriteBoardView
    {
        private const float PAN_HINT_CYCLE = 1.6f, PAN_HINT_FADE = .3f;

        private RectTransform _panHint;
        private Image _panHand;
        private TextMeshProUGUI _panText;
        private bool _panHintOn;
        private float _panHintStart, _panHintTime, _panHintAlpha;

        /// <summary>The line under the hand, in the language in use; the screen sets it.</summary>
        public string PanHintText { get => _panHintText; set { _panHintText = value; if (_panText != null) _panText.text = value ?? ""; } }
        private string _panHintText = "";

        /// <summary>A new level was fitted: its hint starts if it asks for one and the map can slide.</summary>
        private void StartPanHint()
        {
            _panHintOn = _level.PanHint && CanPan;
            _panHintStart = _pan; _panHintTime = 0; _panHintAlpha = 0;
            if (_panHintOn) BuildPanHint();
            if (_panHint != null) { _panHint.gameObject.SetActive(_panHintOn); _panHint.SetAsLastSibling(); }
        }

        private void BuildPanHint()
        {
            if (_panHint != null) return;
            _panHint = new GameObject("PanHint", typeof(RectTransform)).GetComponent<RectTransform>();
            _panHint.gameObject.layer = gameObject.layer;
            _panHint.SetParent(transform, false);
            _panHint.anchorMin = Vector2.zero; _panHint.anchorMax = Vector2.one; _panHint.offsetMin = _panHint.offsetMax = Vector2.zero;
            _panHint.gameObject.AddComponent<Canvas>();
            _panHand = new GameObject("Img_PanHand", typeof(RectTransform)).AddComponent<Image>();
            _panHand.gameObject.layer = gameObject.layer;
            _panHand.rectTransform.SetParent(_panHint, false);
            _panHand.raycastTarget = false; _panHand.preserveAspect = true;
            _panText = new GameObject("Txt_PanHint", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _panText.gameObject.layer = gameObject.layer;
            _panText.rectTransform.SetParent(_panHint, false);
            _panText.raycastTarget = false;
            _panText.alignment = TextAlignmentOptions.Center;
            _panText.enableWordWrapping = true;
            _panText.text = _panHintText ?? "";
        }

        /// <summary>Called each frame: the hand sweeps, and the hint fades out once the map has moved a cell.</summary>
        private void StepPanHint(float dt)
        {
            if (_panHint == null || !_panHint.gameObject.activeSelf) return;
            if (_panHintOn && Mathf.Abs(_pan - _panHintStart) >= _cell) _panHintOn = false;
            _panHintTime += dt;
            _panHintAlpha = Mathf.MoveTowards(_panHintAlpha, _panHintOn ? 1 : 0, dt / PAN_HINT_FADE);
            if (!_panHintOn && _panHintAlpha <= 0) { _panHint.gameObject.SetActive(false); return; }

            var area = _rect.rect;
            float size = _cell * Look.TutorialHandSize;
            _panHand.sprite = Look.TutorialHandSprite;
            _panHand.enabled = Look.TutorialHandSprite != null;
            _panHand.rectTransform.pivot = Look.TutorialHandTip;
            _panHand.rectTransform.sizeDelta = Vector2.one * size;
            // Right to left, the way the map is slid to see what lies to the right; a rest at each end.
            float t = Mathf.Repeat(_panHintTime / PAN_HINT_CYCLE, 1);
            float sweep = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.15f, .75f, t));
            float x = Mathf.Lerp(area.width * .28f, -area.width * .28f, sweep);
            _panHand.rectTransform.anchoredPosition = new Vector2(x, 0);
            float handAlpha = t < .1f ? t / .1f : t > .85f ? (1 - t) / .15f : 1;
            _panHand.color = new Color(1, 1, 1, handAlpha * _panHintAlpha);

            if (Look.TutorialTextFont != null) _panText.font = Look.TutorialTextFont;
            _panText.fontSize = Look.TutorialTextSize;
            var colour = Look.TutorialTextColour; colour.a *= _panHintAlpha;
            _panText.color = colour;
            _panText.outlineColor = Look.TutorialTextOutline;
            _panText.outlineWidth = Look.TutorialTextOutline.a > 0 ? Look.TutorialTextOutlineWidth : 0;
            _panText.rectTransform.sizeDelta = new Vector2(area.width * Look.TutorialTextWidth, Look.TutorialTextSize * 3);
            // Just under the map's bottom wall, where nothing is drawn, so it does not cover the jellies.
            // The row under the floor is the bottom wall, read where it is drawn.
            float mapBottom = area.yMin;
            if (CellCorners(new Vector2(0, -.15f), out var min, out _)) mapBottom = _panHint.InverseTransformPoint(min).y;
            // Hung from its top edge, so a second line grows downward, away from the map.
            _panText.rectTransform.pivot = new Vector2(.5f, 1);
            _panText.alignment = TextAlignmentOptions.Top;
            _panText.rectTransform.anchoredPosition = new Vector2(0, mapBottom);
        }

        private void ClearPanHint()
        {
            _panHintOn = false;
            if (_panHint != null) _panHint.gameObject.SetActive(false);
        }
    }
}
