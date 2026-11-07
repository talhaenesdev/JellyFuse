using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.DevToolsModule.DevToolsScreenModule.ViewsMediators
{
    /// <summary>
    /// A colour on a tuning page is three sliders keyed "X.r", "X.g", "X.b". Above them sits a strip: the colour as
    /// it stands, and a palette to tap - a tap sets the three channels at once, the sliders stay for fine steps.
    /// </summary>
    public sealed partial class DevToolsScreenView
    {
        private const int SWATCH_ROWS = 2;

        /// <summary>Colours to start from: the game's purples and blues first, then the rest of the wheel, greys last.</summary>
        private static readonly Color32[] PALETTE =
        {
            new(0x2A, 0x0A, 0x5E, 0xFF), new(0x4B, 0x1F, 0xA0, 0xFF), new(0x6E, 0x22, 0xD6, 0xFF), new(0xB4, 0x8C, 0xF5, 0xFF),
            new(0x08, 0x1C, 0x4E, 0xFF), new(0x00, 0x8F, 0xFF, 0xFF), new(0x1E, 0xB4, 0xF2, 0xFF), new(0x9C, 0xE6, 0xFF, 0xFF),
            new(0x0E, 0x6B, 0x5C, 0xFF), new(0x2E, 0xC4, 0x6A, 0xFF), new(0xB6, 0xF0, 0x6B, 0xFF), new(0xFF, 0xD2, 0x3F, 0xFF),
            new(0xFF, 0x8A, 0x2B, 0xFF), new(0xE8, 0x3A, 0x4C, 0xFF), new(0xFF, 0x5E, 0x8A, 0xFF), new(0xFF, 0xB8, 0xD8, 0xFF),
            new(0x14, 0x10, 0x1E, 0xFF), new(0x4A, 0x44, 0x5C, 0xFF), new(0x9A, 0x96, 0xAA, 0xFF), new(0xFF, 0xFF, 0xFF, 0xFF),
        };

        private readonly Dictionary<string, Image> _colourPreviews = new();

        /// <summary>The colour field a key names ("Look.Wall.r" → "Look.Wall"), or null for a key that is no channel.</summary>
        private static string ColourBase(string key) =>
            key.Length > 2 && key[key.Length - 2] == '.' && "rgb".IndexOf(key[key.Length - 1]) >= 0 ? key.Substring(0, key.Length - 2) : null;

        /// <summary>The strip for one colour, laid at the list's current row: a preview on the left, the palette beside it.</summary>
        private void AddColourStrip(string colourKey)
        {
            float height = _sliderRowHeight * SWATCH_ROWS;
            var strip = new GameObject("Row_Colour_" + colourKey, typeof(RectTransform)).GetComponent<RectTransform>();
            strip.SetParent(_list, false);
            strip.anchorMin = new Vector2(0, 1); strip.anchorMax = new Vector2(1, 1); strip.pivot = new Vector2(.5f, 1);
            strip.sizeDelta = new Vector2(0, height);
            strip.anchoredPosition = new Vector2(0, -_rowCount * _sliderRowHeight);
            _rowCount += SWATCH_ROWS;

            var preview = Swatch("Img_Preview", strip, Color.white, false);
            var previewRect = preview.rectTransform;
            previewRect.anchorMin = new Vector2(0, 0); previewRect.anchorMax = new Vector2(0, 1); previewRect.pivot = new Vector2(0, .5f);
            previewRect.sizeDelta = new Vector2(height - 12, -12);
            previewRect.anchoredPosition = new Vector2(6, 0);
            _colourPreviews[colourKey] = preview;

            // The palette fills the space right of the preview: two rows of equal cells.
            var palette = new GameObject("Palette", typeof(RectTransform)).GetComponent<RectTransform>();
            palette.SetParent(strip, false);
            palette.anchorMin = Vector2.zero; palette.anchorMax = Vector2.one;
            palette.offsetMin = new Vector2(height + 6, 0); palette.offsetMax = new Vector2(-6, 0);
            int columns = Mathf.CeilToInt(PALETTE.Length / 2f);
            for (int i = 0; i < PALETTE.Length; i++)
            {
                var swatch = Swatch("Btn_Swatch_" + i, palette, PALETTE[i], true);
                int column = i % columns, row = i / columns;
                var rect = swatch.rectTransform;
                rect.anchorMin = new Vector2((float)column / columns, row == 0 ? .5f : 0);
                rect.anchorMax = new Vector2((float)(column + 1) / columns, row == 0 ? 1 : .5f);
                rect.offsetMin = new Vector2(3, 3); rect.offsetMax = new Vector2(-3, -3);
                Color picked = PALETTE[i];
                swatch.GetComponent<Button>().onClick.AddListener(() => PickColour(colourKey, picked));
            }
        }

        private Image Swatch(string name, Transform parent, Color colour, bool button)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline)).GetComponent<Image>();
            image.rectTransform.SetParent(parent, false);
            image.sprite = _sliderSprite;
            image.type = _sliderSprite != null && _sliderSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = colour;
            image.raycastTarget = button;
            var outline = image.GetComponent<Outline>();
            outline.effectColor = new Color(1, 1, 1, .6f);
            outline.effectDistance = new Vector2(2, -2);
            if (button) image.gameObject.AddComponent<Button>().targetGraphic = image;
            return image;
        }

        /// <summary>A tapped swatch: the three sliders move to it, and each sends its value as a drag would.</summary>
        private void PickColour(string colourKey, Color colour)
        {
            for (int channel = 0; channel < 3; channel++)
            {
                string key = colourKey + "." + "rgb"[channel];
                if (!_sliders.TryGetValue(key, out var row)) continue;
                float value = Mathf.Round(colour[channel] * 255);
                _settingSliders = true;
                row.Slider.value = value;
                _settingSliders = false;
                SliderChanged?.Invoke(key, value);
            }
            RefreshPreview(colourKey);
        }

        /// <summary>The preview of one colour, read off its three sliders.</summary>
        private void RefreshPreview(string colourKey)
        {
            if (colourKey == null || !_colourPreviews.TryGetValue(colourKey, out var preview)) return;
            var colour = Color.black;
            for (int channel = 0; channel < 3; channel++)
                if (_sliders.TryGetValue(colourKey + "." + "rgb"[channel], out var row))
                    colour[channel] = row.Slider.value / Mathf.Max(1, row.Slider.maxValue);
            preview.color = colour;
        }

        /// <summary>A channel slider's fill in its own channel's colour, so R, G and B read at a glance.</summary>
        private static void TintChannel(Slider slider, string key)
        {
            int channel = "rgb".IndexOf(key[key.Length - 1]);
            if (slider.fillRect == null) return;
            var fill = slider.fillRect.GetComponent<Image>();
            if (fill != null) fill.color = channel == 0 ? new Color(.95f, .3f, .3f) : channel == 1 ? new Color(.3f, .85f, .4f) : new Color(.35f, .55f, 1f);
        }
    }
}
