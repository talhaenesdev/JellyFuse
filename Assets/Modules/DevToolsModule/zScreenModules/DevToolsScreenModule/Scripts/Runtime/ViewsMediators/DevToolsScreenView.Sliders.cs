using System;
using System.Collections.Generic;
using Modules.DevToolsModule.Data.ValueObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.DevToolsModule.DevToolsScreenModule.ViewsMediators
{
    /// <summary>
    /// The tuning pages - animation and map - each a list of sliders for the values someone sent, built
    /// in code. The main page's content is hidden while one shows; Back brings it back.
    /// </summary>
    public sealed partial class DevToolsScreenView
    {
        [Header("Tuning page")]
        [Tooltip("Opens the animation tuning page.")]
        [SerializeField] private Button _slidersButton;
        [SerializeField] private TMP_Text _slidersLabel;
        [Tooltip("What the main page shows, hidden while the tuning page is open.")]
        [SerializeField] private List<GameObject> _mainPage = new();
        [Tooltip("The area the tuning page fills.")]
        [SerializeField] private RectTransform _slidersArea;
        [SerializeField] private Sprite _sliderSprite;
        [SerializeField, Min(20)] private float _sliderRowHeight = 58;

        /// <summary>A tuning page opened: its name ("animation").</summary>
        public Action<string> SlidersOpened;
        /// <summary>A slider moved: its key and value.</summary>
        public Action<string, float> SliderChanged;
        /// <summary>Reset was pressed on the open tuning page: its name.</summary>
        public Action<string> SlidersResetClicked;

        private RectTransform _page;
        private RectTransform _list;
        private TMP_Text _emptyHint;
        private Button _back, _reset;
        private readonly Dictionary<string, (Slider Slider, TMP_Text Value, DevSliderVO Data)> _sliders = new();
        private bool _settingSliders;
        private string _pageName;
        public const string PAGE_ANIMATION = "animation";

        private void WireSliders(bool on)
        {
            if (_slidersButton == null) return;
            if (on) _slidersButton.onClick.AddListener(OpenAnimation); else _slidersButton.onClick.RemoveListener(OpenAnimation);
            if (_back != null) { if (on) _back.onClick.AddListener(CloseSliders); else _back.onClick.RemoveListener(CloseSliders); }
            if (_reset != null) { if (on) _reset.onClick.AddListener(OnResetSliders); else _reset.onClick.RemoveListener(OnResetSliders); }
            if (!on) CloseSliders();
        }

        private void OpenAnimation() => OpenSliders(PAGE_ANIMATION);
        private void OpenSliders(string page)
        {
            BuildPage();
            // Each page lists its own values: the rows of the last one go.
            if (_pageName != page)
            {
                for (int i = _list.childCount - 1; i >= 0; i--) Destroy(_list.GetChild(i).gameObject);
                _sliders.Clear();
                _list.sizeDelta = new Vector2(_list.sizeDelta.x, 0);
                _pageName = page;
            }
            foreach (var go in _mainPage) if (go != null) go.SetActive(false);
            _page.gameObject.SetActive(true);
            _emptyHint.gameObject.SetActive(_sliders.Count == 0);
            SlidersOpened?.Invoke(page);
        }
        private void OnResetSliders()
        {
            if (!string.IsNullOrEmpty(_pageName)) SlidersResetClicked?.Invoke(_pageName);
        }
        private void CloseSliders()
        {
            if (_page == null || !_page.gameObject.activeSelf) return;
            _page.gameObject.SetActive(false);
            foreach (var go in _mainPage) if (go != null) go.SetActive(true);
        }

        /// <summary>The values to show; a slider already built keeps its place and takes the new value.</summary>
        public void ShowSliders(string page, IReadOnlyList<DevSliderVO> sliders)
        {
            if (page != _pageName) return;
            BuildPage();
            _settingSliders = true;
            foreach (var data in sliders)
            {
                if (!_sliders.TryGetValue(data.Key, out var row)) row = _sliders[data.Key] = AddRow(data);
                row.Slider.minValue = data.Min; row.Slider.maxValue = data.Max; row.Slider.wholeNumbers = data.Whole;
                row.Slider.value = data.Value;
                row.Value.text = Format(data, data.Value);
                _sliders[data.Key] = (row.Slider, row.Value, data);
            }
            _settingSliders = false;
            _emptyHint.gameObject.SetActive(_sliders.Count == 0);
            _list.sizeDelta = new Vector2(_list.sizeDelta.x, _sliders.Count * _sliderRowHeight);
        }

        private static string Format(DevSliderVO data, float value) => data.Whole ? Mathf.RoundToInt(value).ToString() : value.ToString("0.###");

        private void BuildPage()
        {
            if (_page != null) return;
            var font = _title != null ? _title.font : TMP_Settings.defaultFontAsset;
            _page = new GameObject("Pnl_Sliders", typeof(RectTransform)).GetComponent<RectTransform>();
            _page.SetParent(_slidersArea, false);
            _page.anchorMin = Vector2.zero; _page.anchorMax = Vector2.one; _page.offsetMin = _page.offsetMax = Vector2.zero;

            // A scrolling list above, Back below.
            var view = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            view.SetParent(_page, false);
            view.anchorMin = new Vector2(0, 0); view.anchorMax = Vector2.one; view.offsetMin = new Vector2(0, 92); view.offsetMax = Vector2.zero;
            view.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            _list = new GameObject("List", typeof(RectTransform)).GetComponent<RectTransform>();
            _list.SetParent(view, false);
            _list.anchorMin = new Vector2(0, 1); _list.anchorMax = new Vector2(1, 1); _list.pivot = new Vector2(.5f, 1);
            _list.sizeDelta = Vector2.zero;
            var scroll = _page.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = view; scroll.content = _list; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;

            _emptyHint = Text("Txt_Empty", view, font, 24, TextAlignmentOptions.Center);
            _emptyHint.rectTransform.anchorMin = Vector2.zero; _emptyHint.rectTransform.anchorMax = Vector2.one;
            _emptyHint.text = L("devtools.sliders_empty", "Open this page from inside a level.");

            // Back: a copy of the page's own button.
            if (_slidersButton != null)
            {
                _back = Instantiate(_slidersButton, _page);
                _back.name = "Btn_Back";
                var rect = (RectTransform)_back.transform;
                // Back on the left half, Reset on the right.
                rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(.5f, 0); rect.pivot = new Vector2(.5f, 0);
                rect.anchoredPosition = Vector2.zero; rect.offsetMin = new Vector2(0, 0); rect.offsetMax = new Vector2(-6, 72);
                var label = _back.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = L("devtools.back", "Back");
                _back.onClick.AddListener(CloseSliders);

                _reset = Instantiate(_slidersButton, _page);
                _reset.name = "Btn_Reset";
                var resetRect = (RectTransform)_reset.transform;
                resetRect.anchorMin = new Vector2(.5f, 0); resetRect.anchorMax = new Vector2(1, 0); resetRect.pivot = new Vector2(.5f, 0);
                resetRect.anchoredPosition = Vector2.zero; resetRect.offsetMin = new Vector2(6, 0); resetRect.offsetMax = new Vector2(0, 72);
                var resetLabel = _reset.GetComponentInChildren<TMP_Text>(true);
                if (resetLabel != null) resetLabel.text = L("devtools.reset", "Reset");
                _reset.onClick.AddListener(OnResetSliders);
            }
            _page.gameObject.SetActive(false);
        }

        private (Slider, TMP_Text, DevSliderVO) AddRow(DevSliderVO data)
        {
            var font = _title != null ? _title.font : TMP_Settings.defaultFontAsset;
            var row = new GameObject("Row_" + data.Key, typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(_list, false);
            row.anchorMin = new Vector2(0, 1); row.anchorMax = new Vector2(1, 1); row.pivot = new Vector2(.5f, 1);
            row.sizeDelta = new Vector2(0, _sliderRowHeight);
            row.anchoredPosition = new Vector2(0, -_sliders.Count * _sliderRowHeight);

            var label = Text("Txt_Label", row, font, 20, TextAlignmentOptions.Left);
            label.rectTransform.anchorMin = new Vector2(0, .5f); label.rectTransform.anchorMax = new Vector2(.75f, 1);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.text = L("devtools.slider." + data.Key, data.Label);
            var value = Text("Txt_Value", row, font, 20, TextAlignmentOptions.Right);
            value.rectTransform.anchorMin = new Vector2(.75f, .5f); value.rectTransform.anchorMax = new Vector2(1, 1);
            value.rectTransform.offsetMin = value.rectTransform.offsetMax = Vector2.zero;

            var resources = new DefaultControls.Resources { standard = _sliderSprite, background = _sliderSprite, knob = _sliderSprite };
            var sliderGo = DefaultControls.CreateSlider(resources);
            var sliderRect = (RectTransform)sliderGo.transform;
            sliderRect.SetParent(row, false);
            sliderRect.anchorMin = new Vector2(0, 0); sliderRect.anchorMax = new Vector2(1, .5f);
            sliderRect.offsetMin = new Vector2(6, 4); sliderRect.offsetMax = new Vector2(-6, -4);
            var slider = sliderGo.GetComponent<Slider>();
            string key = data.Key;
            slider.onValueChanged.AddListener(v =>
            {
                if (!_sliders.TryGetValue(key, out var entry)) return;
                entry.Value.text = Format(entry.Data, v);
                if (!_settingSliders) SliderChanged?.Invoke(key, v);
            });
            return (slider, value, data);
        }

        private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, float size, TextAlignmentOptions align)
        {
            var text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(parent, false);
            text.font = font; text.fontSize = size; text.alignment = align; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }
    }
}
