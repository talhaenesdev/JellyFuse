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
        [SerializeField] private RectTransform _playerRunner;
        [SerializeField] private RectTransform _opponentRunner;
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

        [SerializeField] private Sprite _sentSprite;

        [Header("Challenge received")]
        [SerializeField] private GameObject _challenge;
        [SerializeField] private Image _challengeFromIcon;
        [SerializeField] private Image _challengeMeIcon;
        [SerializeField] private TMP_Text _challengeTitle;
        [SerializeField] private TMP_Text _challengeBody;
        [SerializeField] private Button _challengeAccept;
        [SerializeField] private Button _challengeDecline;

        [Header("Challenge outcome")]
        [SerializeField] private GameObject _notice;
        [SerializeField] private Image _noticeIcon;
        [SerializeField] private TMP_Text _noticeText;

        public Action CloseClicked;
        public Action<string> ChallengeAccepted;
        public Action<string> ChallengeDeclined;
        /// <summary>The outcome notice has had its time and nothing else is on show.</summary>
        public Action Idle;

        private string _challengeId = string.Empty;
        private string _playerIcon = string.Empty;
        private float _noticeUntil = -1;
        private const float NOTICE_SECONDS = 4f;

        private Func<string, string> _localize;
        private CD_ProfileIcons _icons;
        private IReadOnlyList<float> _opponentExits = Array.Empty<float>();
        private int _total = 1;
        private float _countdownLeft;
        private float _raceStart = -1;
        private float _playerProgress;
        private float _shownPlayer;
        private bool _racing;
        private bool _counting;
        /// <summary>Seconds of 3-2-1; any countdown longer than this shows the opponent found first.</summary>
        private const float COUNT = 3f;
        private const float GO_SECONDS = 0.7f;
        /// <summary>Where on the track a full bar ends, short of the finish flag.</summary>
        private const float FILL_END = 0.956f;
        /// <summary>The shortest a shown bar is, its two rounded ends side by side.</summary>
        private const float FILL_MIN = 0.06f;

        private void OnEnable()
        {
            if (_searchClose != null) _searchClose.onClick.AddListener(OnCloseClicked);
            if (_resultClose != null) _resultClose.onClick.AddListener(OnResultClosed);
            if (_challengeAccept != null) _challengeAccept.onClick.AddListener(OnAccept);
            if (_challengeDecline != null) _challengeDecline.onClick.AddListener(OnDecline);
        }

        private void OnAccept() { CloseChallenge(); ChallengeAccepted?.Invoke(_challengeId); }

        private void OnDecline()
        {
            CloseChallenge();
            ChallengeDeclined?.Invoke(_challengeId);
            if (NothingElse && _noticeUntil < 0) Idle?.Invoke();
        }

        private void CloseChallenge()
        {
            if (_challenge != null) _challenge.SetActive(false);
            if (_blocker != null) _blocker.SetActive(false);
        }

        /// <summary>Somebody challenged this player: who, and on which level, with Accept and Decline.</summary>
        public void ShowChallenge(ChallengeInviteRVO invite)
        {
            _challengeId = invite.Id;
            SetIcon(_challengeFromIcon, invite.FromIcon);
            SetIcon(_challengeMeIcon, _playerIcon);
            if (_challengeTitle != null) _challengeTitle.text = L("duel.challenge_title", "Challenge!");
            if (_challengeBody != null) _challengeBody.text = string.Format(L("duel.challenge_body", "{0} challenged you on level {1}"), invite.FromName, invite.Level);
            Label(_challengeAccept, L("duel.accept", "Play"));
            Label(_challengeDecline, L("duel.decline", "Later"));
            if (_challenge != null) _challenge.SetActive(true);
            if (_blocker != null) _blocker.SetActive(true);
        }

        private static void Label(Button button, string text)
        {
            var label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null) label.text = text;
        }

        /// <summary>How a challenge this player sent went, in a banner along the top for a few seconds.</summary>
        public void ShowOutcome(ChallengeOutcomeRVO outcome)
        {
            SetIcon(_noticeIcon, outcome.OpponentIcon);
            string text = outcome.Declined ? L("duel.outcome_declined", "{0} turned down your challenge")
                : outcome.SenderWon ? L("duel.outcome_won", "You beat {0} in your challenge!")
                : L("duel.outcome_lost", "{0} beat your challenge");
            if (_noticeText != null) _noticeText.text = string.Format(text, outcome.OpponentName);
            if (_notice != null) _notice.SetActive(true);
            _noticeUntil = Time.realtimeSinceStartup + NOTICE_SECONDS;
        }

        /// <summary>Nothing of the duel is on show but the notice, if that.</summary>
        private bool NothingElse => (_search == null || !_search.activeSelf) && (_race == null || !_race.activeSelf)
            && (_challenge == null || !_challenge.activeSelf) && (_result == null || !_result.activeSelf);

        private void OnDisable()
        {
            if (_searchClose != null) _searchClose.onClick.RemoveListener(OnCloseClicked);
            if (_resultClose != null) _resultClose.onClick.RemoveListener(OnResultClosed);
            if (_challengeAccept != null) _challengeAccept.onClick.RemoveListener(OnAccept);
            if (_challengeDecline != null) _challengeDecline.onClick.RemoveListener(OnDecline);
        }

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            _racing = false;
            _raceStart = -1;
            _countdownLeft = 0;
            _counting = false;
            _noticeUntil = -1;
            Show(search: false, countdown: false, race: false, result: false);
            if (_challenge != null) _challenge.SetActive(false);
            if (_notice != null) _notice.SetActive(false);
        }

        private void OnCloseClicked() => CloseClicked?.Invoke();
        private void OnResultClosed() { if (_result != null) _result.SetActive(false); }

        public void SetLocalizer(Func<string, string> localize) => _localize = localize;

        public void SetProfile(CD_ProfileIcons icons, string playerName, string playerIcon)
        {
            _icons = icons;
            _playerIcon = playerIcon ?? string.Empty;
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
            if (_searchSpinner != null) _searchSpinner.gameObject.SetActive(true);
        }

        public void ShowUnavailable()
        {
            Show(search: true, countdown: false, race: false, result: false);
            if (_searchStatus != null) _searchStatus.text = L("duel.unavailable", "No opponent found. Win more levels or check your connection.");
            if (_searchClose != null) _searchClose.gameObject.SetActive(true);
        }

        /// <summary>
        /// The opponent found: their face shows in the search panel for whatever of the countdown is more than 3-2-1,
        /// then the numbers run over the blocked board, then the race bar takes over.
        /// </summary>
        public void StartRace(DuelRaceRVO race)
        {
            _opponentExits = race.OpponentExits ?? Array.Empty<float>();
            _total = Mathf.Max(1, race.Total);
            _playerProgress = _shownPlayer = 0;
            SetIcon(_searchOpponentIcon, race.OpponentIcon);
            SetIcon(_raceOpponentIcon, race.OpponentIcon);
            if (_opponentName != null) _opponentName.text = race.OpponentName;
            if (_searchStatus != null) _searchStatus.text = string.Format(L("duel.found", "Opponent found: {0}"), race.OpponentName);
            if (_searchSpinner != null) _searchSpinner.gameObject.SetActive(false);
            SetFill(_playerFill, _playerRunner, 0);
            SetFill(_opponentFill, _opponentRunner, 0);
            _countdownLeft = race.Countdown;
            _raceStart = Time.realtimeSinceStartup + race.Countdown;
            _counting = race.Countdown > 0;
            _racing = true;
            bool reveal = race.Countdown > COUNT;
            Show(search: reveal, countdown: _counting && !reveal, race: true, result: false);
        }

        public void ApplyProgress(float progress) => _playerProgress = Mathf.Clamp01(progress);

        public void ShowResult(DuelResultRVO result)
        {
            _racing = false;
            if (result.Seconds >= 0) _playerProgress = 1;
            if (_resultBanner != null) _resultBanner.sprite = result.ChallengeSent && _sentSprite != null ? _sentSprite : result.Won ? _winSprite : _loseSprite;
            if (_resultTitle != null) _resultTitle.text = result.ChallengeSent ? L("duel.sent", "Challenge sent!") : result.Won ? L("duel.won", "You won!") : L("duel.lost", "You lost!");
            if (result.ChallengeSent && _resultTimes != null)
                _resultTimes.text = string.Format(L("duel.sent_body", "Your time {0} - {1} will race it"), Seconds(result.Seconds), result.ChallengedName);
            else if (_resultTimes != null)
            {
                _resultTimes.text = result.Seconds >= 0
                    ? string.Format(L("duel.times", "You {0}  ·  Rival {1}"), Seconds(result.Seconds), Seconds(result.OpponentSeconds))
                    : string.Format(L("duel.rival_time", "Rival finished in {0}"), Seconds(result.OpponentSeconds));
            }
            if (_reward != null) _reward.SetActive(result.Coins > 0);
            if (_rewardCoins != null) _rewardCoins.text = "+" + result.Coins;
            if (_result != null) _result.SetActive(true);
            if (_blocker != null) _blocker.SetActive(false);
        }

        private void Update()
        {
            if (_searchSpinner != null && _searchSpinner.gameObject.activeInHierarchy) _searchSpinner.Rotate(0, 0, -180f * Time.unscaledDeltaTime);
            if (_noticeUntil > 0 && Time.realtimeSinceStartup >= _noticeUntil)
            {
                _noticeUntil = -1;
                if (_notice != null) _notice.SetActive(false);
                if (NothingElse) Idle?.Invoke();
            }
            if (_raceStart < 0) return;
            float now = Time.realtimeSinceStartup;
            if (_counting)
            {
                _countdownLeft = _raceStart - now;
                bool numbers = _countdownLeft <= COUNT;
                if (numbers && _search != null && _search.activeSelf)
                {
                    _search.SetActive(false);
                    if (_countdown != null) _countdown.gameObject.SetActive(true);
                }
                if (numbers && _countdown != null && _countdownSprites != null && _countdownSprites.Length >= 4)
                {
                    int step = _countdownLeft > 0 ? Mathf.Clamp(Mathf.CeilToInt(_countdownLeft), 1, 3) : 0;
                    _countdown.sprite = _countdownSprites[step == 0 ? 3 : 3 - step];
                    float within = _countdownLeft > 0 ? _countdownLeft - Mathf.Floor(_countdownLeft) : 1 + _countdownLeft / GO_SECONDS;
                    _countdown.rectTransform.localScale = Vector3.one * (0.8f + 0.4f * Mathf.Clamp01(within));
                }
                // GO stays a moment after the race starts, then the board is the player's.
                if (_countdownLeft <= -GO_SECONDS)
                {
                    _counting = false;
                    if (_countdown != null) _countdown.gameObject.SetActive(false);
                    if (_blocker != null) _blocker.SetActive(false);
                }
            }
            if (_racing) SetFill(_opponentFill, _opponentRunner, OpponentProgress(now - _raceStart));
            if (!Mathf.Approximately(_shownPlayer, _playerProgress))
            {
                _shownPlayer = Mathf.MoveTowards(_shownPlayer, _playerProgress, Time.unscaledDeltaTime * 2f);
                SetFill(_playerFill, _playerRunner, _shownPlayer);
            }
        }

        /// <summary>
        /// A sliced bar grown to its share of the groove, so its rounded ends keep their shape - hidden while too short to
        /// draw - and the runner riding at its head.
        /// </summary>
        private static void SetFill(Image fill, RectTransform runner, float share)
        {
            if (fill == null) return;
            share = Mathf.Clamp01(share);
            RectTransform rect = fill.rectTransform;
            float head = Mathf.Lerp(rect.anchorMin.x + FILL_MIN, FILL_END, share);
            rect.anchorMax = new Vector2(head, rect.anchorMax.y);
            fill.enabled = share > 0.02f;
            if (runner == null) return;
            runner.anchorMin = new Vector2(Mathf.Max(head, rect.anchorMin.x + FILL_MIN * 0.5f), runner.anchorMin.y);
            runner.anchorMax = new Vector2(runner.anchorMin.x, runner.anchorMax.y);
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
            if (_blocker != null) _blocker.SetActive(search || countdown || (_challenge != null && _challenge.activeSelf));
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
