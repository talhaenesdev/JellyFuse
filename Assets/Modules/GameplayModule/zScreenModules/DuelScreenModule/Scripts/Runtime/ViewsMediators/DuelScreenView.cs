using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.ProfileModule.Shared.Data.UnityObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.DuelScreenModule.ViewsMediators
{
    /// <summary>
    /// The duel overlay. Four parts, one shown at a time but the race bar: the search panel while an opponent is found,
    /// the 3-2-1-GO countdown, the race bar along the top while the board is played, and the result banner. The
    /// opponent's bar is drawn from their recorded run against the clock; the player's moves when Gameplay says so.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class DuelScreenView : ScreenView
    {
        [Header("Search")]
        [SerializeField] private GameObject _search;
        [SerializeField] private Image _searchPlayerIcon;
        [SerializeField] private Image _searchOpponentIcon;
        [SerializeField] private TMP_Text _searchStatus;
        [SerializeField] private Button _searchClose;
        [SerializeField] private RectTransform _searchSpinner;

        [Header("Countdown")]
        [SerializeField] private GameObject _blocker;
        [SerializeField] private Image _countdown;
        [SerializeField] private Sprite[] _countdownSprites = new Sprite[4];

        [Header("Race")]
        [SerializeField] private GameObject _race;
        [SerializeField] private Image _playerFill;
        [SerializeField] private Image _opponentFill;
        [SerializeField] private Image _raceOpponentIcon;
        [SerializeField] private Image _racePlayerIcon;
        [SerializeField] private TMP_Text _opponentName;
        [SerializeField] private TMP_Text _playerName;

        [Header("Result")]
        [SerializeField] private GameObject _result;
        [SerializeField] private Button _resultClose;
        [SerializeField] private Image _resultBanner;
        [SerializeField] private Sprite _winSprite;
        [SerializeField] private Sprite _loseSprite;
        [SerializeField] private TMP_Text _resultTitle;
        [SerializeField] private TMP_Text _resultTimes;
        [SerializeField] private GameObject _reward;
        [SerializeField] private TMP_Text _rewardCoins;

        public Action CloseClicked;

        private Func<string, string> _localize;
        private CD_ProfileIcons _icons;
        private IReadOnlyList<float> _opponentExits = Array.Empty<float>();
        private int _total = 1;
        private float _countdownLeft;
        private float _raceStart = -1;
        private float _playerProgress;
        private float _shownPlayer;
        private bool _racing;

        private void OnEnable()
        {
            if (_searchClose != null) _searchClose.onClick.AddListener(OnCloseClicked);
            if (_resultClose != null) _resultClose.onClick.AddListener(OnResultClosed);
        }

        private void OnDisable()
        {
            if (_searchClose != null) _searchClose.onClick.RemoveListener(OnCloseClicked);
            if (_resultClose != null) _resultClose.onClick.RemoveListener(OnResultClosed);
        }

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            _racing = false;
            _raceStart = -1;
            _countdownLeft = 0;
            Show(search: false, countdown: false, race: false, result: false);
        }

        private void OnCloseClicked() => CloseClicked?.Invoke();
        private void OnResultClosed() { if (_result != null) _result.SetActive(false); }

        public void SetLocalizer(Func<string, string> localize) => _localize = localize;

        public void SetProfile(CD_ProfileIcons icons, string playerName, string playerIcon)
        {
            _icons = icons;
            SetIcon(_searchPlayerIcon, playerIcon);
            SetIcon(_racePlayerIcon, playerIcon);
            if (_playerName != null) _playerName.text = string.IsNullOrEmpty(playerName) ? L("duel.you", "You") : playerName;
        }

        /// <summary>The search panel, an opponent being looked for.</summary>
        public void ShowSearch()
        {
            Show(search: true, countdown: false, race: false, result: false);
            if (_searchOpponentIcon != null) _searchOpponentIcon.enabled = false;
            if (_searchStatus != null) _searchStatus.text = L("duel.searching", "Finding an opponent...");
            if (_searchClose != null) _searchClose.gameObject.SetActive(false);
        }

        public void ShowUnavailable()
        {
            Show(search: true, countdown: false, race: false, result: false);
            if (_searchStatus != null) _searchStatus.text = L("duel.unavailable", "No opponent found. Win more levels or check your connection.");
            if (_searchClose != null) _searchClose.gameObject.SetActive(true);
        }

        /// <summary>The opponent found: the countdown runs, the board blocked, then the race bar takes over.</summary>
        public void StartRace(DuelRaceRVO race)
        {
            _opponentExits = race.OpponentExits ?? Array.Empty<float>();
            _total = Mathf.Max(1, race.Total);
            _playerProgress = _shownPlayer = 0;
            SetIcon(_searchOpponentIcon, race.OpponentIcon);
            SetIcon(_raceOpponentIcon, race.OpponentIcon);
            if (_opponentName != null) _opponentName.text = race.OpponentName;
            if (_playerFill != null) _playerFill.fillAmount = 0;
            if (_opponentFill != null) _opponentFill.fillAmount = 0;
            _countdownLeft = race.Countdown;
            _raceStart = Time.realtimeSinceStartup + race.Countdown;
            _racing = true;
            Show(search: false, countdown: race.Countdown > 0, race: true, result: false);
        }

        public void ApplyProgress(float progress) => _playerProgress = Mathf.Clamp01(progress);

        public void ShowResult(DuelResultRVO result)
        {
            _racing = false;
            if (result.Seconds >= 0) _playerProgress = 1;
            if (_resultBanner != null) _resultBanner.sprite = result.Won ? _winSprite : _loseSprite;
            if (_resultTitle != null) _resultTitle.text = result.Won ? L("duel.won", "You won!") : L("duel.lost", "You lost!");
            if (_resultTimes != null)
            {
                string mine = result.Seconds >= 0 ? Seconds(result.Seconds) : "-";
                _resultTimes.text = string.Format(L("duel.times", "You {0}  ·  Rival {1}"), mine, Seconds(result.OpponentSeconds));
            }
            if (_reward != null) _reward.SetActive(result.Coins > 0);
            if (_rewardCoins != null) _rewardCoins.text = "+" + result.Coins;
            if (_result != null) _result.SetActive(true);
            if (_blocker != null) _blocker.SetActive(false);
        }

        private void Update()
        {
            if (_searchSpinner != null && _search != null && _search.activeSelf) _searchSpinner.Rotate(0, 0, -180f * Time.unscaledDeltaTime);
            if (_raceStart < 0) return;
            float now = Time.realtimeSinceStartup;
            if (_countdownLeft > 0)
            {
                _countdownLeft = _raceStart - now;
                int step = _countdownLeft > 0 ? Mathf.Clamp(Mathf.CeilToInt(_countdownLeft), 1, 3) : 0;
                if (_countdown != null && _countdownSprites != null && _countdownSprites.Length >= 4)
                {
                    _countdown.sprite = _countdownSprites[step == 0 ? 3 : 3 - step];
                    float within = _countdownLeft > 0 ? _countdownLeft - Mathf.Floor(_countdownLeft) : 1 + _countdownLeft;
                    _countdown.rectTransform.localScale = Vector3.one * (0.8f + 0.4f * Mathf.Clamp01(within));
                }
                if (_blocker != null) _blocker.SetActive(_countdownLeft > -0.6f);
                if (_countdownLeft <= -0.6f && _countdown != null) _countdown.gameObject.SetActive(false);
            }
            if (_racing && _opponentFill != null) _opponentFill.fillAmount = OpponentProgress(now - _raceStart);
            if (_playerFill != null && !Mathf.Approximately(_shownPlayer, _playerProgress))
            {
                _shownPlayer = Mathf.MoveTowards(_shownPlayer, _playerProgress, Time.unscaledDeltaTime * 2f);
                _playerFill.fillAmount = _shownPlayer;
            }
        }

        /// <summary>The opponent's share at this second of the race, eased between their recorded exits.</summary>
        private float OpponentProgress(float second)
        {
            if (second <= 0 || _opponentExits.Count == 0) return 0;
            float previous = 0;
            for (int i = 0; i < _opponentExits.Count; i++)
            {
                float at = _opponentExits[i];
                if (second < at)
                {
                    float within = at > previous ? (second - previous) / (at - previous) : 1;
                    return Mathf.Clamp01((i + within) / _opponentExits.Count);
                }
                previous = at;
            }
            return 1;
        }

        private void Show(bool search, bool countdown, bool race, bool result)
        {
            if (_search != null) _search.SetActive(search);
            if (_blocker != null) _blocker.SetActive(search || countdown);
            if (_countdown != null) _countdown.gameObject.SetActive(countdown);
            if (_race != null) _race.SetActive(race);
            if (_result != null) _result.SetActive(result);
        }

        private void SetIcon(Image image, string id)
        {
            if (image == null) return;
            Sprite sprite = null;
            if (_icons != null && _icons.Icons.Count > 0)
            {
                foreach (var icon in _icons.Icons)
                    if (icon != null && icon.Id == id) { sprite = icon.Sprite; break; }
                if (sprite == null && _icons.Icons[0] != null) sprite = _icons.Icons[0].Sprite;
            }
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private static string Seconds(float seconds) => seconds >= 3600 ? "-" : $"{seconds:0.0}s";

        private string L(string key, string english)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? english : text;
        }

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
