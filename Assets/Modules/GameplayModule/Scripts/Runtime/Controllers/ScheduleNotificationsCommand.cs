using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Constants;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.LocalizationModule.Services;
using Modules.NotificationModule.Services;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Sets the game's notifications from where the player is now: one when the lives will be full again, while
    /// some are missing, and the come-back reminders, moved on from now each time the player is seen.
    /// </summary>
    internal class ScheduleNotificationsCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private INotificationService _notifications { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_Lives>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (config == null || progress == null) return;

            // Lives full: from the next refill, one refill for every LivesPerRefill still missing after it.
            if (progress.Lives < config.MaxLives && progress.NextLifeAt > 0)
            {
                int refills = (config.MaxLives - progress.Lives + config.LivesPerRefill - 1) / config.LivesPerRefill;
                long full = progress.NextLifeAt + (refills - 1) * config.RefillSeconds;
                _notifications.Schedule(GameplayNotifications.LIVES_FULL,
                    Text("notification.lives_full.title", "Lives refilled!"),
                    Text("notification.lives_full.text", "Your lives are full. Time to fuse some jellies!"),
                    DateTimeOffset.FromUnixTimeSeconds(full).LocalDateTime);
            }
            else _notifications.Cancel(GameplayNotifications.LIVES_FULL);

            var now = DateTime.Now;
            _notifications.Schedule(GameplayNotifications.COME_BACK_SOON,
                Text("notification.come_back_soon.title", "The jellies miss you"),
                Text("notification.come_back_soon.text", "A new level is waiting. Come back and play!"),
                now.AddHours(GameplayNotifications.SOON_HOURS));
            _notifications.Schedule(GameplayNotifications.COME_BACK_LATER,
                Text("notification.come_back_later.title", "Still there?"),
                Text("notification.come_back_later.text", "Your jellies are getting bored. Jump back in!"),
                now.AddHours(GameplayNotifications.LATER_HOURS));
        }

        private string Text(string key, string english)
        {
            string text = _localization?.Get(key);
            return string.IsNullOrEmpty(text) || text == key ? english : text;
        }
    }
}
