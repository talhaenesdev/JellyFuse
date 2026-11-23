using System;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.MainModule.EventsScreenModule.ViewsMediators
{
    /// <summary>
    /// The Color Carnival card: today's colour, its points against the next chest, how long the day has left, and one
    /// button - Open while a chest waits, Play otherwise. A chest's prize shows on the card for a moment.
    /// </summary>
    public partial class EventsScreenView
    {
        public Action CarnivalOpenClicked;
        public Action CarnivalPlayClicked;

        private bool _carnivalClaimable;
        private Button _carnivalButton;
        private float _carnivalPrizeUntil;

        private void ApplyCarnival(EventsRVO events)
        {
            var card = _eventList.content.Find("Pnl_Event_ColorCarnival");
            if (card == null) return;
            var group = card.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1;
            var chests = events.CarnivalChests;
            bool done = chests == null || events.CarnivalOpened >= chests.Count;
            var next = done ? null : chests[events.CarnivalOpened];
            _carnivalClaimable = events.CarnivalClaimable;
            string colour = L("colour." + events.CarnivalColour.ToString().ToLowerInvariant(), events.CarnivalColour.ToString());

            if (Time.unscaledTime > _carnivalPrizeUntil)
                Set(card, "Txt_Description", done
                    ? L("events.colorcarnival.done", "All of today's chests are open!")
                    : string.Format(L("events.colorcarnival.progress", "Today's colour: {0} - {1} / {2}"), colour, Mathf.Min(events.CarnivalPoints, next.Points), next.Points));
            ShowRewardText(card, next == null ? string.Empty : L("events.colorcarnival.chest" + (events.CarnivalOpened + 1), "Chest " + (events.CarnivalOpened + 1)));
            Set(card, "Pnl_Time/Txt_Time", TimeLeft(events.CarnivalEndsAt));
            Set(card, "Btn_PlayEvent/Txt_Play", _carnivalClaimable ? L("events.open", "Open") : L("events.play", "Play"));

            var button = card.Find("Btn_PlayEvent")?.GetComponent<Button>();
            if (button != null && button != _carnivalButton)
            {
                if (_carnivalButton != null) _carnivalButton.onClick.RemoveListener(OnCarnivalClicked);
                _carnivalButton = button;
                _carnivalButton.onClick.AddListener(OnCarnivalClicked);
            }
            if (button != null) button.interactable = !done || _carnivalClaimable;
            if (!_carnivalClaimable) ShowLocked(card, button, events.Level, events.EventsUnlockLevel);
        }

        /// <summary>What a chest just gave, on the card for a few seconds.</summary>
        public void ShowCarnivalPrize(CarnivalPrizeRVO prize)
        {
            if (prize == null || _eventList == null || _eventList.content == null) return;
            var card = _eventList.content.Find("Pnl_Event_ColorCarnival");
            if (card == null) return;
            var parts = new System.Collections.Generic.List<string>();
            if (prize.Coins > 0) parts.Add(string.Format(L("events.starrush.coins", "{0} coins"), prize.Coins));
            if (prize.Rockets > 0) parts.Add(string.Format(L("events.starrush.rockets", "{0} rocket"), prize.Rockets));
            if (prize.Freezes > 0) parts.Add(string.Format(L("events.starrush.freezes", "{0} freeze"), prize.Freezes));
            if (prize.Ufos > 0) parts.Add(string.Format(L("events.starrush.ufos", "{0} UFO"), prize.Ufos));
            if (prize.Lives > 0) parts.Add(string.Format(L("events.starrush.lives", "{0} lives"), prize.Lives));
            Set(card, "Txt_Description", string.Format(L("events.colorcarnival.prize", "You got {0}!"), string.Join(" + ", parts)));
            _carnivalPrizeUntil = Time.unscaledTime + 3f;
        }

        private void OnCarnivalClicked()
        {
            if (_carnivalClaimable) CarnivalOpenClicked?.Invoke();
            else CarnivalPlayClicked?.Invoke();
        }
    }
}
