using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.LeaderboardModule.Data.ValueObjects;
using Modules.LeaderboardModule.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.MainModule.LeaderboardScreenModule.ViewsMediators
{
    /// <summary>
    /// Two tabs, all-time and weekly, over a list of the top players, and the player's own row pinned under it.
    /// Rows are copies of a template row in the prefab, kept between openings and reused.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class LeaderboardScreenView : ScreenView
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _allTimeTab;
        [SerializeField] private Button _weeklyTab;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _allTimeLabel;
        [SerializeField] private TMP_Text _weeklyLabel;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _content;
        [Tooltip("A row inside the list, switched off: every row shown is a copy of it.")]
        [SerializeField] private RectTransform _rowTemplate;
        [Tooltip("The player's own row, under the list.")]
        [SerializeField] private RectTransform _meRow;
        [SerializeField] private Color _tabOn = new(1f, .82f, .25f);
        [SerializeField] private Color _tabOff = new(.42f, .2f, .78f);
        [SerializeField] private Color _rowColour = new(.26f, .06f, .63f, .88f);
        [SerializeField] private Color _myRowColour = new(.12f, .7f, .95f, 1f);

        public Action BackClicked;
        public Action<LeaderboardBoard> TabClicked;

        private readonly List<RectTransform> _rows = new();
        private Func<string, string> _localize;
        private LeaderboardBoard _board;

        private void OnEnable()
        {
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);
            if (_allTimeTab != null) _allTimeTab.onClick.AddListener(OnAllTimeClicked);
            if (_weeklyTab != null) _weeklyTab.onClick.AddListener(OnWeeklyClicked);
        }

        private void OnDisable()
        {
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClicked);
            if (_allTimeTab != null) _allTimeTab.onClick.RemoveListener(OnAllTimeClicked);
            if (_weeklyTab != null) _weeklyTab.onClick.RemoveListener(OnWeeklyClicked);
        }

        private void OnBackClicked() => BackClicked?.Invoke();
        private void OnAllTimeClicked() => TabClicked?.Invoke(LeaderboardBoard.AllTime);
        private void OnWeeklyClicked() => TabClicked?.Invoke(LeaderboardBoard.Weekly);

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>The function that turns a key into text in the language in use.</summary>
        public void SetLocalizer(Func<string, string> localize)
        {
            _localize = localize;
            if (_title != null) _title.text = L("leaderboard.title", "LEADERBOARD");
            if (_allTimeLabel != null) _allTimeLabel.text = L("leaderboard.all_time", "All Time");
            if (_weeklyLabel != null) _weeklyLabel.text = L("leaderboard.weekly", "Weekly");
        }

        /// <summary>The board's tab lit, its rows cleared and a line saying it is on its way.</summary>
        public void ShowLoading(LeaderboardBoard board)
        {
            _board = board;
            PaintTabs();
            HideRows();
            if (_meRow != null) _meRow.gameObject.SetActive(false);
            Status(L("leaderboard.loading", "Loading..."));
        }

        /// <summary>The board that came back; a board other than the one picked since is ignored.</summary>
        public void ShowBoard(LeaderboardBoard board, LeaderboardPageVO page)
        {
            if (board != _board) return;
            HideRows();
            if (page == null) { Status(L("leaderboard.offline", "Can't reach the leaderboard. Check your connection.")); return; }
            if (page.Top.Count == 0) Status(L("leaderboard.empty", "No scores yet. Win a level to be the first!"));
            else Status(string.Empty);
            for (int i = 0; i < page.Top.Count; i++) Fill(Row(i), page.Top[i]);
            if (_meRow != null)
            {
                _meRow.gameObject.SetActive(page.Me != null);
                if (page.Me != null) Fill(_meRow, page.Me);
            }
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
        }

        private RectTransform Row(int index)
        {
            while (_rows.Count <= index)
            {
                var row = Instantiate(_rowTemplate, _content);
                row.name = "Row_" + _rows.Count;
                _rows.Add(row);
            }
            _rows[index].gameObject.SetActive(true);
            return _rows[index];
        }

        private void Fill(RectTransform row, LeaderboardRowVO data)
        {
            Set(row, "Txt_Rank", data.Rank.ToString());
            Set(row, "Txt_Name", string.IsNullOrEmpty(data.Name) ? L("leaderboard.player", "Player") : TrimName(data.Name));
            Set(row, "Txt_Score", data.Score.ToString("N0"));
            var image = row.GetComponent<Image>();
            if (image != null && row != _meRow) image.color = data.IsMe ? _myRowColour : _rowColour;
        }

        // Unity Authentication adds "#1234" to a player's name so names stay unique; the list shows the name alone.
        private static string TrimName(string name)
        {
            int hash = name.LastIndexOf('#');
            return hash > 0 ? name.Substring(0, hash) : name;
        }

        private static void Set(Transform row, string child, string value)
        {
            var text = row.Find(child)?.GetComponent<TMP_Text>();
            if (text != null) text.text = value;
        }

        private void HideRows()
        {
            foreach (var row in _rows) row.gameObject.SetActive(false);
        }

        private void PaintTabs()
        {
            if (_allTimeTab != null) _allTimeTab.image.color = _board == LeaderboardBoard.AllTime ? _tabOn : _tabOff;
            if (_weeklyTab != null) _weeklyTab.image.color = _board == LeaderboardBoard.Weekly ? _tabOn : _tabOff;
        }

        private void Status(string text)
        {
            if (_status == null) return;
            _status.text = text;
            _status.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        private string L(string key, string english)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? english : text;
        }

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
