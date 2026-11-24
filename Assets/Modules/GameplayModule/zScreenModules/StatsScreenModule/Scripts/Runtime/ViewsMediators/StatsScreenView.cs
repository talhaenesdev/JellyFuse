using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.UiWidgetsModule.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.StatsScreenModule.ViewsMediators
{
    /// <summary>
    /// The stats popup: one row per total, a label and its number. The rows are laid out in the prefab in the order
    /// Rows lists them; the view only writes the words and the numbers.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class StatsScreenView : ScreenView
    {
        /// <summary>The totals, in the order the prefab's rows show them.</summary>
        public enum Row
        {
            Taps = 0,
            Merges = 1,
            Delivered = 2,
            LevelsWon = 3,
            BoostersUsed = 4,
            BestWinStreak = 5,
        }

        [Tooltip("The popup, authored at its own size and scaled to fit the screen.")]
        [SerializeField] private RectTransform _content;
        [SerializeField, Range(.3f, 1)] private float _widthShare = .9f;
        [SerializeField, Range(.3f, 1)] private float _heightShare = .82f;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _backdropButton;
        [Tooltip("Each row's label, in Row order.")]
        [SerializeField] private TMP_Text[] _labels;
        [Tooltip("Each row's number, in Row order.")]
        [SerializeField] private TMP_Text[] _values;

        public Action CloseClicked;

        private Func<string, string> _localize;

        private void OnRectTransformDimensionsChange() => FitLayout();

        private void FitLayout()
        {
            if (_content == null) return;
            var rect = ((RectTransform)transform).rect;
            var size = _content.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            _content.localScale = Vector3.one * Mathf.Max(.01f, Mathf.Min(rect.width * _widthShare / size.x, rect.height * _heightShare / size.y));
        }

        private void OnEnable()
        {
            FitLayout();
            if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDisable()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
            if (_backdropButton != null) _backdropButton.onClick.RemoveListener(OnCloseClicked);
        }

        public void SetLocalizer(Func<string, string> localize) => _localize = localize;

        public void ShowStats(JellyStatsSVO stats)
        {
            stats ??= new JellyStatsSVO();
            // Fixed words are LocalizedTextViews in the prefab, edited in the inspector: stats.title.
            Set(Row.Taps, "stats.taps", "Jellies tapped", stats.Taps);
            Set(Row.Merges, "stats.merges", "Jellies merged", stats.Merges);
            Set(Row.Delivered, "stats.delivered", "Jellies through doors", stats.Delivered);
            Set(Row.LevelsWon, "stats.levels_won", "Levels won", stats.LevelsWon);
            Set(Row.BoostersUsed, "stats.boosters_used", "Boosters used", stats.BoostersUsed);
            Set(Row.BestWinStreak, "stats.best_streak", "Best win streak", stats.BestWinStreak);
        }

        private void Set(Row row, string key, string fallback, long value)
        {
            int i = (int)row;
            if (_labels != null && i < _labels.Length && _labels[i] != null) _labels[i].text = L(key, fallback);
            if (_values != null && i < _values.Length && _values[i] != null) _values[i].text = value.ToString("N0");
        }

        private string L(string key, string fallback)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? fallback : text;
        }

        private void OnCloseClicked() => CloseClicked?.Invoke();

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);

        protected override void PlayHideAnimation()
        {
            // The panel shrinks and fades out first; the screen is hidden once it has.
            var panel = GetComponentInChildren<PanelAnimator>();
            if (panel != null) panel.Close(() => HideCompleted?.Invoke(this));
            else HideCompleted?.Invoke(this);
        }
    }
}
