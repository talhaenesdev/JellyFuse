using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.DailyQuestsScreenModule.Entities;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.UiWidgetsModule.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.DailyQuestsScreenModule.ViewsMediators
{
    [RequireComponent(typeof(ViewInjector))]
    public class DailyQuestsScreenView : ScreenView
    {
        [Tooltip("The popup, authored at its own size and scaled to fit the screen.")]
        [SerializeField] private RectTransform _content;
        [SerializeField, Range(.3f, 1)] private float _widthShare = .9f;
        [SerializeField, Range(.3f, 1)] private float _heightShare = .82f;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _timer;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _backdropButton;
        [Tooltip("The first row, laid out in the prefab; one more is copied from it for each quest past the first.")]
        [SerializeField] private QuestRowWidget _rowTemplate;
        [Tooltip("Plays a claimed reward flying off its row: coins to the counter, the rest upwards.")]
        [SerializeField] private RewardFlyer _flyer;
        [SerializeField] private Sprite _coinIcon;
        [Tooltip("The booster icons in BoosterType order: Rocket, Freeze Time, Ufo.")]
        [SerializeField] private Sprite[] _boosterIcons;

        public Action CloseClicked;
        public Action<int> ClaimClicked;

        private readonly List<QuestRowWidget> _rows = new();
        // What each row showed last, so a quest that has just been claimed is seen flying off.
        private readonly List<(QuestType Type, bool Claimed)> _shown = new();
        private Func<string, string> _localize;
        private long _resetAt;
        private int _shownSeconds = -1;

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
            _closeButton.onClick.AddListener(OnCloseClicked);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            if (_backdropButton != null) _backdropButton.onClick.RemoveListener(OnCloseClicked);
        }

        public void SetLocalizer(Func<string, string> localize) => _localize = localize;

        public void ShowEvents(EventsRVO events)
        {
            if (_rows.Count == 0) _rows.Add(_rowTemplate);
            _title.text = L("quests.title", "Daily Quests");
            var quests = events.Quests;
            while (_rows.Count < quests.Count)
            {
                var row = Instantiate(_rowTemplate, _rowTemplate.transform.parent);
                _rows.Add(row);
            }
            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < quests.Count;
                _rows[i].gameObject.SetActive(used);
                if (!used) continue;
                int index = i;
                _rows[i].Claimed = () => ClaimClicked?.Invoke(index);
                _rows[i].Show(quests[i], L);
                bool justClaimed = quests[i].Claimed && i < _shown.Count && _shown[i].Type == quests[i].Type && !_shown[i].Claimed;
                if (justClaimed && _flyer != null) FlyReward(quests[i].Reward, _rows[i].RewardAnchor);
            }
            _shown.Clear();
            foreach (var quest in quests) _shown.Add((quest.Type, quest.Claimed));
            _resetAt = events.QuestsResetAt;
            _shownSeconds = -1;
            RefreshTimer();
        }

        private void Update() => RefreshTimer();

        // The countdown is only drawn; the quests are replaced by Gameplay when the day turns.
        private void RefreshTimer()
        {
            if (_timer == null || _resetAt <= 0) return;
            int seconds = (int)Math.Max(0, _resetAt - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            if (seconds == _shownSeconds) return;
            _shownSeconds = seconds;
            var time = TimeSpan.FromSeconds(seconds);
            _timer.text = string.Format(L("quests.resets_in", "New quests in {0}"), $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}");
        }

        private void FlyReward(RewardCVO reward, RectTransform from)
        {
            int booster = (int)reward.Booster;
            bool coins = reward.Coins > 0;
            var icon = coins || _boosterIcons == null || booster >= _boosterIcons.Length ? _coinIcon : _boosterIcons[booster];
            _flyer.Fly(icon, from, coins, Mathf.Clamp(reward.Coins / 10, 4, 12));
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
