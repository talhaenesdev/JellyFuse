using System;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.MainModule.EventsScreenModule.ViewsMediators
{
    /// <summary>
    /// The Star Rush card: the week's stars against the next step, what that step pays, how long the week has left,
    /// and one button - Claim while a step waits, Play otherwise.
    /// </summary>
    public partial class EventsScreenView
    {
        [Header("Star Rush")]
        [Tooltip("The meter's golden fill, a horizontally filled image over the track.")]
        [SerializeField] private Image _starFill;
        [Tooltip("One box per reward step, in order; each is placed along the meter at its step's stars.")]
        [SerializeField] private Image[] _starBoxes = new Image[0];
        [SerializeField] private Sprite[] _starBoxClosed = new Sprite[0];
        [SerializeField] private Sprite[] _starBoxReady = new Sprite[0];
        [SerializeField] private Sprite[] _starBoxOpen = new Sprite[0];
        [Tooltip("The card's corner mark while a step waits to be claimed.")]
        [SerializeField] private GameObject _starBadge;

        public Action StarRushClaimClicked;
        public Action StarRushPlayClicked;

        private Func<string, string> _localize;
        private EventsRVO _events;
        private bool _starRushClaimable;
        private Button _starRushButton;

        public void ApplyEvents(EventsRVO events)
        {
            _events = events;
            if (events == null || _eventList == null || _eventList.content == null) return;
            var card = _eventList.content.Find("Pnl_Event_StarRush");
            if (card == null) return;
            // Open now: no longer faded like the events still to come.
            var group = card.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1;
            var steps = events.StarRushMilestones;
            int stars = events.StarRushStars;
            bool done = steps == null || events.StarRushClaimed >= steps.Count;
            var next = done ? null : steps[events.StarRushClaimed];
            _starRushClaimable = events.StarRushClaimable;

            Set(card, "Txt_Description", done
                ? L("events.starrush.done", "All rewards taken this week!")
                : string.Format(L("events.starrush.progress", "{0} / {1} stars"), Mathf.Min(stars, next.Stars), next.Stars));
            ShowMeter(events);
            ApplyCarnival(events);
            Set(card, "Pnl_Reward/Txt_Reward", next == null ? string.Empty : Reward(next));
            Set(card, "Pnl_Time/Txt_Time", TimeLeft(events.StarRushEndsAt));
            Set(card, "Btn_PlayEvent/Txt_Play", _starRushClaimable ? L("events.claim", "Claim") : L("events.play", "Play"));

            var button = card.Find("Btn_PlayEvent")?.GetComponent<Button>();
            if (button != null && button != _starRushButton)
            {
                if (_starRushButton != null) _starRushButton.onClick.RemoveListener(OnStarRushClicked);
                _starRushButton = button;
                _starRushButton.onClick.AddListener(OnStarRushClicked);
            }
            if (button != null) button.interactable = !done || _starRushClaimable;
        }

        /// <summary>The meter: filled to the week's stars against the last step, a box at each step - open once paid, glowing once reached.</summary>
        private void ShowMeter(EventsRVO events)
        {
            var steps = events.StarRushMilestones;
            int last = steps != null && steps.Count > 0 ? Mathf.Max(1, steps[steps.Count - 1].Stars) : 1;
            if (_starFill != null) _starFill.fillAmount = Mathf.Clamp01(events.StarRushStars / (float)last);
            for (int i = 0; i < _starBoxes.Length; i++)
            {
                var box = _starBoxes[i];
                if (box == null) continue;
                bool exists = steps != null && i < steps.Count;
                box.gameObject.SetActive(exists);
                if (!exists) continue;
                float at = Mathf.Clamp01(steps[i].Stars / (float)last);
                var rect = box.rectTransform;
                rect.anchorMin = new Vector2(at, rect.anchorMin.y); rect.anchorMax = new Vector2(at, rect.anchorMax.y);
                bool paid = i < events.StarRushClaimed;
                bool reached = !paid && events.StarRushStars >= steps[i].Stars;
                var set = paid ? _starBoxOpen : reached ? _starBoxReady : _starBoxClosed;
                if (i < set.Length && set[i] != null) box.sprite = set[i];
            }
            if (_starBadge != null) _starBadge.SetActive(events.StarRushClaimable);
        }

        private void OnStarRushClicked()
        {
            if (_starRushClaimable) StarRushClaimClicked?.Invoke();
            else StarRushPlayClicked?.Invoke();
        }

        private string Reward(StarRushMilestoneCVO step)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (step.Coins > 0) parts.Add(string.Format(L("events.starrush.coins", "{0} coins"), step.Coins));
            if (step.Rockets > 0) parts.Add(string.Format(L("events.starrush.rockets", "{0} rocket"), step.Rockets));
            if (step.Freezes > 0) parts.Add(string.Format(L("events.starrush.freezes", "{0} freeze"), step.Freezes));
            if (step.Ufos > 0) parts.Add(string.Format(L("events.starrush.ufos", "{0} UFO"), step.Ufos));
            if (step.Lives > 0) parts.Add(string.Format(L("events.starrush.lives", "{0} lives"), step.Lives));
            return string.Join(" + ", parts);
        }

        private string TimeLeft(long endsAt)
        {
            var left = TimeSpan.FromSeconds(Math.Max(0, endsAt - DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            return left.TotalDays >= 1
                ? string.Format(L("events.time_days", "{0}d {1}h"), (int)left.TotalDays, left.Hours)
                : string.Format(L("events.time_hours", "{0}h {1}m"), left.Hours, left.Minutes);
        }

        private string L(string key, string english)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? english : text;
        }

        private static void Set(Transform root, string path, string value)
        {
            var text = root.Find(path)?.GetComponent<TMP_Text>();
            if (text != null) text.text = value;
        }
    }
}
