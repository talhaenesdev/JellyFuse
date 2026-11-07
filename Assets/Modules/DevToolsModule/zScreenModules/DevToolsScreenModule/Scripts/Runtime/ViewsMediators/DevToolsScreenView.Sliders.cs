using System;
using System.Collections.Generic;
using Modules.DevToolsModule.Data.ValueObjects;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [Tooltip("Opens the look tuning page: the colours of the walls and the grid.")]
        [SerializeField] private Button _lookButton;
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
        private int _rowCount;
        private string _lastGroup;
        public const string PAGE_ANIMATION = "animation";
        public const string PAGE_LOOK = "look";
        private CanvasGroup _hideGroup;

        private void WireSliders(bool on)
        {
            if (_slidersButton == null) return;
            if (on) _slidersButton.onClick.AddListener(OpenAnimation); else _slidersButton.onClick.RemoveListener(OpenAnimation);
            if (_lookButton != null) { if (on) _lookButton.onClick.AddListener(OpenLook); else _lookButton.onClick.RemoveListener(OpenLook); }
            if (!on) ShowPanelWhileDragging(true);
            if (_back != null) { if (on) _back.onClick.AddListener(CloseSliders); else _back.onClick.RemoveListener(CloseSliders); }
            if (_reset != null) { if (on) _reset.onClick.AddListener(OnResetSliders); else _reset.onClick.RemoveListener(OnResetSliders); }
            if (!on) CloseSliders();
        }

        private void OpenAnimation() => OpenSliders(PAGE_ANIMATION);
        private void OpenLook() => OpenSliders(PAGE_LOOK);

        /// <summary>
        /// The whole panel goes see-through while a slider is held, so what it changes shows behind it,
        /// and comes back when it is let go. It still takes the touch, so the drag carries on.
        /// </summary>
        private void ShowPanelWhileDragging(bool shown)
        {
            if (_hideGroup == null)
            {
                if (shown) return;
                _hideGroup = GetComponent<CanvasGroup>();
                if (_hideGroup == null) _hideGroup = gameObject.AddComponent<CanvasGroup>();
            }
            _hideGroup.alpha = shown ? 1 : 0;
        }
        private void OpenSliders(string page)
        {
            BuildPage();
            // Each page lists its own values: the rows of the last one go.
            if (_pageName != page)
            {
                for (int i = _list.childCount - 1; i >= 0; i--) Destroy(_list.GetChild(i).gameObject);
                _sliders.Clear();
                _colourPreviews.Clear();
                _list.sizeDelta = new Vector2(_list.sizeDelta.x, 0);
                _rowCount = 0; _lastGroup = null;
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
            foreach (var key in _colourPreviews.Keys) RefreshPreview(key);
            _emptyHint.gameObject.SetActive(_sliders.Count == 0);
            _list.sizeDelta = new Vector2(_list.sizeDelta.x, _rowCount * _sliderRowHeight);
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
            if (!string.IsNullOrEmpty(data.Group) && data.Group != _lastGroup)
            {
                // A heading where the section changes: the sliders under it belong to one thing on screen.
                var heading = Text("Txt_Group_" + data.Group, _list, font, 26, TextAlignmentOptions.BottomLeft);
                heading.fontStyle = FontStyles.Bold;
                heading.rectTransform.anchorMin = new Vector2(0, 1); heading.rectTransform.anchorMax = new Vector2(1, 1); heading.rectTransform.pivot = new Vector2(.5f, 1);
                heading.rectTransform.sizeDelta = new Vector2(0, _sliderRowHeight);
                heading.rectTransform.anchoredPosition = new Vector2(0, -_rowCount * _sliderRowHeight);
                heading.text = L("devtools.group." + data.Group, data.Group);
                _rowCount++;
            }
            _lastGroup = data.Group;
            string colourKey = ColourBase(data.Key);
            // A colour's strip - its preview and palette - comes before its first channel.
            if (colourKey != null && data.Key.EndsWith(".r")) AddColourStrip(colourKey);
            var row = new GameObject("Row_" + data.Key, typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(_list, false);
            row.anchorMin = new Vector2(0, 1); row.anchorMax = new Vector2(1, 1); row.pivot = new Vector2(.5f, 1);
            row.sizeDelta = new Vector2(0, _sliderRowHeight);
            row.anchoredPosition = new Vector2(0, -_rowCount * _sliderRowHeight);
            _rowCount++;

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
                RefreshPreview(colourKey);
            });
            if (colourKey != null) TintChannel(slider, key);
            var trigger = sliderGo.AddComponent<EventTrigger>();
            // The held row stays in sight over the board, on a dark strip, so its value can be read while the rest is hidden.
            var rowGroup = row.gameObject.AddComponent<CanvasGroup>();
            var strip = row.gameObject.AddComponent<Image>();
            strip.color = new Color(0, 0, 0, .7f);
            strip.raycastTarget = false;
            strip.enabled = false;
            AddTrigger(trigger, EventTriggerType.PointerDown, () => { rowGroup.ignoreParentGroups = true; strip.enabled = true; ShowPanelWhileDragging(false); });
            AddTrigger(trigger, EventTriggerType.PointerUp, () => { rowGroup.ignoreParentGroups = false; strip.enabled = false; ShowPanelWhileDragging(true); });
            return (slider, value, data);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, float size, TextAlignmentOptions align)
        {
            var text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(parent, false);
            text.font = font; text.fontSize = size; text.alignment = align; text.raycastTarget = false;
            // Overflow, not Ellipsis: a row's half is shorter than the font's line, and Ellipsis then hides the whole text.
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }
    }
}
