using System;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.MainModule.EventsScreenModule.ViewsMediators
{
    /// <summary>
    /// The Speed Run card: five levels on one clock, what finishing pays, and one button - Start with a try left, an ad
    /// for one more when the day's are used, or nothing until tomorrow.
    /// </summary>
    public partial class EventsScreenView
    {
        public Action SpeedRunStartClicked;
        public Action SpeedRunAdClicked;

        private enum SpeedRunButton { Start = 0, Ad = 1, None = 2 }
        private SpeedRunButton _speedRunButtonMode;
        private Button _speedRunButton;

        private void ApplySpeedRun(EventsRVO events)
        {
            var card = _eventList.content.Find("Pnl_Event_SpeedRun");
            if (card == null) return;
            var group = card.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1;
            _speedRunButtonMode = events.SpeedRunTriesLeft > 0 ? SpeedRunButton.Start : events.SpeedRunAdLeft > 0 ? SpeedRunButton.Ad : SpeedRunButton.None;

            Set(card, "Txt_Description", events.SpeedRunWon
                ? L("events.speedrun.won", "You finished today's run!")
                : string.Format(L("events.speedrun.desc_count", "Finish {0} levels before the clock runs out."), events.SpeedRunLevels));
            ShowRewardIcons(card, events.SpeedRunCoins, events.SpeedRunRockets, events.SpeedRunFreezes, 0, 0);
            Set(card, "Pnl_Time/Txt_Time", TimeLeft(new DateTimeOffset(DateTime.Now.Date.AddDays(1)).ToUnixTimeSeconds()));
            Set(card, "Btn_PlayEvent/Txt_Play", _speedRunButtonMode == SpeedRunButton.Start ? L("events.start", "Start")
                : _speedRunButtonMode == SpeedRunButton.Ad ? L("events.speedrun.ad", "Watch ad")
                : L("events.tomorrow", "Tomorrow"));

            var button = card.Find("Btn_PlayEvent")?.GetComponent<Button>();
            if (button != null && button != _speedRunButton)
            {
                if (_speedRunButton != null) _speedRunButton.onClick.RemoveListener(OnSpeedRunClicked);
                _speedRunButton = button;
                _speedRunButton.onClick.AddListener(OnSpeedRunClicked);
            }
            if (button != null) button.interactable = _speedRunButtonMode != SpeedRunButton.None;
        }

        private void OnSpeedRunClicked()
        {
            if (_speedRunButtonMode == SpeedRunButton.Start) SpeedRunStartClicked?.Invoke();
            else if (_speedRunButtonMode == SpeedRunButton.Ad) SpeedRunAdClicked?.Invoke();
        }
    }
}
