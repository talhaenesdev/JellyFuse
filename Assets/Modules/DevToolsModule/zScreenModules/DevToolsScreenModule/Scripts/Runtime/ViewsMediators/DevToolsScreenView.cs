using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.DevToolsModule.Data.ValueObjects;
using Modules.UiWidgetsModule.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.DevToolsModule.DevToolsScreenModule.ViewsMediators
{
    /// <summary>
    /// The developer panel: one row per switch, built from a hidden row template. A row is built
    /// once per key and kept, so the pooled screen opens again without rebuilding.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public sealed partial class DevToolsScreenView : ScreenView
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _previousLabel;
        [SerializeField] private TMP_Text _nextLabel;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _previousLevelButton;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _resetBoostersButton;
        [SerializeField] private TMP_Text _resetBoostersLabel;
        [SerializeField] private Button _refillLivesButton;
        [SerializeField] private TMP_Text _refillLivesLabel;
        [SerializeField] private Button _resetPlayerButton;
        [SerializeField] private TMP_Text _resetPlayerLabel;
        [Tooltip("Where the rows go, top to bottom.")]
        [SerializeField] private RectTransform _rows;
        [Tooltip("A row: a TMP label and a SlideSwitch. Kept inactive; each switch gets a copy.")]
        [SerializeField] private GameObject _rowTemplate;
        [Tooltip("Height of one row, in the panel's units.")]
        [SerializeField, Min(1)] private float _rowHeight = 64;
        [Header("Switch pages")]
        [Tooltip("The page the switches show on, with its own Back; one page of switches at a time.")]
        [SerializeField] private GameObject _togglesPage;
        [SerializeField] private Button _togglesBack;
        [SerializeField] private TMP_Text _togglesBackLabel;
        [Tooltip("Opens the gameplay rules switches (Page \"game\").")]
        [SerializeField] private Button _gameTogglesButton;
        [SerializeField] private TMP_Text _gameTogglesLabel;
        [Tooltip("Opens the display switches (Page \"display\").")]
        [SerializeField] private Button _displayTogglesButton;
        [SerializeField] private TMP_Text _displayTogglesLabel;

        public Action CloseClicked;
        /// <summary>-1 for the previous level, +1 for the next.</summary>
        public Action<int> SkipLevelClicked;
        public Action ResetBoostersClicked;
        public Action RefillLivesClicked;
        public Action ResetPlayerClicked;
        /// <summary>A switch was asked to change: its key and the value asked for.</summary>
        public Action<string, bool> ToggleRequested;

        private readonly Dictionary<string, SlideSwitch> _switches = new();
        private readonly List<(TMP_Text Label, DevToggleCVO Toggle)> _labels = new();
        private Func<string, string> _localize;

        /// <summary>
        /// The function that turns a key into text in the language in use (ILocalizationService.Get).
        /// A switch's text is keyed "devtools." and its key; its Label in CD_DevTools is the fallback.
        /// </summary>
        public void SetLocalizer(Func<string, string> localize)
        {
            _localize = localize;
            RefreshTexts();
        }

        private void RefreshTexts()
        {
            if (_title != null) _title.text = L("devtools.title", "Dev Tools");
            if (_previousLabel != null) _previousLabel.text = L("devtools.previous", "Previous");
            if (_nextLabel != null) _nextLabel.text = L("devtools.next", "Next");
            if (_resetBoostersLabel != null) _resetBoostersLabel.text = L("devtools.reset_boosters", "Reset Boosters");
            if (_refillLivesLabel != null) _refillLivesLabel.text = L("devtools.refill_lives", "Refill Lives");
            if (_resetPlayerLabel != null) _resetPlayerLabel.text = L("devtools.reset_player", "Reset Player");
            if (_gameTogglesLabel != null) _gameTogglesLabel.text = L("devtools.game_toggles", "Gameplay Settings");
            if (_displayTogglesLabel != null) _displayTogglesLabel.text = L("devtools.display_toggles", "Display Settings");
            if (_togglesBackLabel != null) _togglesBackLabel.text = L("devtools.back", "Back");
            if (_slidersLabel != null) _slidersLabel.text = L("devtools.sliders", "Animation Settings");
            foreach (var (label, toggle) in _labels)
                label.text = L("devtools." + toggle.Key, string.IsNullOrEmpty(toggle.Label) ? toggle.Key : toggle.Label);
        }

        private string L(string key, string fallback)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? fallback : text;
        }

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
            _previousLevelButton.onClick.AddListener(OnPreviousClicked);
            _nextLevelButton.onClick.AddListener(OnNextClicked);
            if (_resetBoostersButton != null) _resetBoostersButton.onClick.AddListener(OnResetBoostersClicked);
            if (_refillLivesButton != null) _refillLivesButton.onClick.AddListener(OnRefillLivesClicked);
            if (_resetPlayerButton != null) _resetPlayerButton.onClick.AddListener(OnResetPlayerClicked);
            WireSliders(true);
            if (_gameTogglesButton != null) _gameTogglesButton.onClick.AddListener(OpenGameToggles);
            if (_displayTogglesButton != null) _displayTogglesButton.onClick.AddListener(OpenDisplayToggles);
            if (_togglesBack != null) _togglesBack.onClick.AddListener(CloseToggles);
        }
        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _previousLevelButton.onClick.RemoveListener(OnPreviousClicked);
            _nextLevelButton.onClick.RemoveListener(OnNextClicked);
            if (_resetBoostersButton != null) _resetBoostersButton.onClick.RemoveListener(OnResetBoostersClicked);
            if (_refillLivesButton != null) _refillLivesButton.onClick.RemoveListener(OnRefillLivesClicked);
            if (_resetPlayerButton != null) _resetPlayerButton.onClick.RemoveListener(OnResetPlayerClicked);
            WireSliders(false);
            if (_gameTogglesButton != null) _gameTogglesButton.onClick.RemoveListener(OpenGameToggles);
            if (_displayTogglesButton != null) _displayTogglesButton.onClick.RemoveListener(OpenDisplayToggles);
            if (_togglesBack != null) _togglesBack.onClick.RemoveListener(CloseToggles);
            CloseToggles();
        }

        public void ShowToggles(IReadOnlyList<DevToggleCVO> toggles)
        {
            _rowTemplate.SetActive(false);
            for (int i = 0; i < toggles.Count; i++)
            {
                var toggle = toggles[i];
                if (_switches.ContainsKey(toggle.Key)) continue;
                var row = Instantiate(_rowTemplate, _rows);
                row.name = "Row_" + toggle.Key;
                row.SetActive(true);
                _rowPages[toggle.Key] = (row, string.IsNullOrEmpty(toggle.Page) ? "game" : toggle.Page);
                var label = row.GetComponentInChildren<TMP_Text>(true);
                if (label != null) _labels.Add((label, toggle));
                var slide = row.GetComponentInChildren<SlideSwitch>(true);
                string key = toggle.Key;
                slide.Requested += on => ToggleRequested?.Invoke(key, on);
                _switches[key] = slide;
            }
            if (_togglesPage != null && _togglesPage.activeSelf) LayRows();
            RefreshTexts();
        }

        private readonly Dictionary<string, (GameObject Row, string Page)> _rowPages = new();
        private string _togglePage;

        private void OpenGameToggles() => OpenToggles("game");
        private void OpenDisplayToggles() => OpenToggles("display");
        /// <summary>Shows the switches of one page, the main page hidden behind it.</summary>
        private void OpenToggles(string page)
        {
            if (_togglesPage == null) return;
            _togglePage = page;
            foreach (var go in _mainPage) if (go != null) go.SetActive(false);
            _togglesPage.SetActive(true);
            LayRows();
        }
        /// <summary>The page's switches top to bottom; the other pages' are hidden.</summary>
        private void LayRows()
        {
            int shown = 0;
            foreach (var entry in _rowPages.Values)
            {
                bool on = entry.Page == _togglePage;
                entry.Row.SetActive(on);
                if (!on) continue;
                var rect = (RectTransform)entry.Row.transform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -_rowHeight * (shown + .5f));
                shown++;
            }
        }
        private void CloseToggles()
        {
            if (_togglesPage == null || !_togglesPage.activeSelf) return;
            _togglesPage.SetActive(false);
            foreach (var go in _mainPage) if (go != null) go.SetActive(true);
        }

        public void ShowToggle(string key, bool on)
        {
            if (_switches.TryGetValue(key, out var slide)) slide.SetOn(on);
        }

        private void OnCloseClicked() => CloseClicked?.Invoke();
        private void OnPreviousClicked() => SkipLevelClicked?.Invoke(-1);
        private void OnNextClicked() => SkipLevelClicked?.Invoke(1);
        private void OnResetBoostersClicked() => ResetBoostersClicked?.Invoke();
        private void OnRefillLivesClicked() => RefillLivesClicked?.Invoke();
        private void OnResetPlayerClicked() => ResetPlayerClicked?.Invoke();

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
