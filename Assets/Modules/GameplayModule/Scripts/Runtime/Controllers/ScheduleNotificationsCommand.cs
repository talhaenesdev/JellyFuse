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
            // A day away: the player's own animal is hungry, by name, when an animal is chosen.
            string animal = AnimalName();
            _notifications.Schedule(GameplayNotifications.COME_BACK_SOON,
                animal != null
                    ? string.Format(Text("notification.hungry.title", "{0} is hungry!"), animal)
                    : Text("notification.come_back_soon.title", "The jellies miss you"),
                animal != null
                    ? Text("notification.hungry.text", "Come fuse the jellies and feed them!")
                    : Text("notification.come_back_soon.text", "A new level is waiting. Come back and play!"),
                now.AddHours(GameplayNotifications.SOON_HOURS));
            _notifications.Schedule(GameplayNotifications.COME_BACK_LATER,
                Text("notification.come_back_later.title", "Still there?"),
                Text("notification.come_back_later.text", "Your jellies are getting bored. Jump back in!"),
                now.AddHours(GameplayNotifications.LATER_HOURS));
        }

        /// <summary>The chosen avatar's animal in the player's language, the cat when none was ever chosen; null when it has no name.</summary>
        private string AnimalName()
        {
            var profile = _shared.GetScriptable<Modules.ProfileModule.Shared.Data.UnityObjects.SD_PlayerProfile>();
            string id = profile != null ? profile.SelectedIconId : null;
            // No avatar ever picked: the game shows the cat, the first one, so the cat is the one hungry.
            if (string.IsNullOrEmpty(id)) id = "pink";
            string key = "animal." + id;
            string name = _localization?.Get(key);
            return string.IsNullOrEmpty(name) || name == key ? null : name;
        }

        private string Text(string key, string english)
        {
            string text = _localization?.Get(key);
            return string.IsNullOrEmpty(text) || text == key ? english : text;
        }
    }
}
