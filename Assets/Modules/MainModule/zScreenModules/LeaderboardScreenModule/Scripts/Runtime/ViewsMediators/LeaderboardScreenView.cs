using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.LeaderboardModule.Data.ValueObjects;
using Modules.LeaderboardModule.Enums;
using Modules.ProfileModule.Shared.Data.UnityObjects;
using Modules.ProfileModule.Shared.Data.ValueObjects;
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
        [Tooltip("The player's own row as a button: it opens the profile to change the name and icon.")]
        [SerializeField] private Button _editProfileButton;
        [SerializeField] private TMP_Text _editProfileLabel;
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
        [Tooltip("The first three players, in the order 1st, 2nd, 3rd; each holds Txt_Name and Txt_Score.")]
        [SerializeField] private RectTransform[] _podium = new RectTransform[0];
        [Tooltip("Rank badges for 1st, 2nd, 3rd and every other place, behind a row's rank.")]
        [SerializeField] private Sprite[] _badges = new Sprite[0];
        [SerializeField] private Sprite _tabOnSprite;
        [SerializeField] private Sprite _tabOffSprite;
        [SerializeField] private Color _tabOn = new(1f, .82f, .25f);
        [SerializeField] private Color _tabOff = new(.42f, .2f, .78f);
        [SerializeField] private Color _rowColour = new(.26f, .06f, .63f, .88f);
        [SerializeField] private Color _myRowColour = new(.12f, .7f, .95f, 1f);

        public Action BackClicked;
        public Action<LeaderboardBoard> TabClicked;
        public Action EditProfileClicked;

        private readonly List<RectTransform> _rows = new();
        private Func<string, string> _localize;
        private LeaderboardBoard _board;
        private CD_ProfileIcons _icons;

        private void OnEnable()
        {
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);
            if (_allTimeTab != null) _allTimeTab.onClick.AddListener(OnAllTimeClicked);
            if (_weeklyTab != null) _weeklyTab.onClick.AddListener(OnWeeklyClicked);
            if (_editProfileButton != null) _editProfileButton.onClick.AddListener(OnEditProfileClicked);
        }

        private void OnDisable()
        {
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClicked);
            if (_allTimeTab != null) _allTimeTab.onClick.RemoveListener(OnAllTimeClicked);
            if (_weeklyTab != null) _weeklyTab.onClick.RemoveListener(OnWeeklyClicked);
            if (_editProfileButton != null) _editProfileButton.onClick.RemoveListener(OnEditProfileClicked);
        }

        private void OnBackClicked() => BackClicked?.Invoke();
        private void OnAllTimeClicked() => TabClicked?.Invoke(LeaderboardBoard.AllTime);
        private void OnWeeklyClicked() => TabClicked?.Invoke(LeaderboardBoard.Weekly);
        private void OnEditProfileClicked() => EditProfileClicked?.Invoke();

        /// <summary>The catalogue a row's icon id is looked up in.</summary>
        public void SetIcons(CD_ProfileIcons icons) => _icons = icons;

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
            if (_editProfileLabel != null) _editProfileLabel.text = L("leaderboard.edit", "Edit");
        }

        /// <summary>The board's tab lit, its rows cleared and a line saying it is on its way.</summary>
        public void ShowLoading(LeaderboardBoard board)
        {
            _board = board;
            PaintTabs();
            HideRows();
            HidePodium();
            if (_meRow != null) _meRow.gameObject.SetActive(false);
            Status(L("leaderboard.loading", "Loading..."));
        }

        /// <summary>The board that came back; a board other than the one picked since is ignored.</summary>
        public void ShowBoard(LeaderboardBoard board, LeaderboardPageVO page)
        {
            if (board != _board) return;
            HideRows();
            HidePodium();
            if (page == null) { Status(L("leaderboard.offline", "Can't reach the leaderboard. Check your connection.")); return; }
            if (page.Top.Count == 0) Status(L("leaderboard.empty", "No scores yet. Win a level to be the first!"));
            else Status(string.Empty);
            // The first places stand on the podium; the list starts after them.
            int onPodium = Mathf.Min(_podium.Length, page.Top.Count);
            for (int i = 0; i < onPodium; i++)
            {
                if (_podium[i] == null) continue;
                _podium[i].gameObject.SetActive(true);
                Fill(_podium[i], page.Top[i]);
            }
            for (int i = onPodium; i < page.Top.Count; i++) Fill(Row(i - onPodium), page.Top[i]);
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
            var avatar = row.Find("Img_Avatar")?.GetComponent<Image>();
            if (avatar != null)
            {
                var icon = Icon(data.Icon);
                avatar.sprite = icon?.Sprite;
                avatar.color = icon == null ? new Color(1, 1, 1, .35f) : icon.Sprite != null ? Color.white : icon.FallbackColor;
            }
            var badge = row.Find("Img_Badge")?.GetComponent<Image>();
            if (badge != null && _badges.Length > 0)
            {
                int last = _badges.Length - 1;
                badge.sprite = _badges[data.Rank >= 1 && data.Rank - 1 < last ? data.Rank - 1 : last];
            }
            var image = row.GetComponent<Image>();
            if (image != null && row != _meRow && !IsPodium(row)) image.color = data.IsMe ? _myRowColour : _rowColour;
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

        /// <summary>The picked icon, or the catalogue's first for a player who never picked one.</summary>
        private ProfileIconCVO Icon(string id)
        {
            if (_icons == null || _icons.Icons.Count == 0) return null;
            foreach (var icon in _icons.Icons)
                if (icon != null && icon.Id == id) return icon;
            return _icons.Icons[0];
        }

        private bool IsPodium(RectTransform row) => Array.IndexOf(_podium, row) >= 0;

        private void HidePodium()
        {
            foreach (var slot in _podium) if (slot != null) slot.gameObject.SetActive(false);
        }

        private void HideRows()
        {
            foreach (var row in _rows) row.gameObject.SetActive(false);
        }

        private void PaintTabs()
        {
            PaintTab(_allTimeTab, _board == LeaderboardBoard.AllTime);
            PaintTab(_weeklyTab, _board == LeaderboardBoard.Weekly);
        }

        private void PaintTab(Button tab, bool on)
        {
            if (tab == null) return;
            if (_tabOnSprite != null && _tabOffSprite != null)
            {
                tab.image.sprite = on ? _tabOnSprite : _tabOffSprite;
                tab.image.color = Color.white;
            }
            else tab.image.color = on ? _tabOn : _tabOff;
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
