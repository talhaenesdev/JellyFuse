using System;
namespace Modules.NotificationModule.Services
{
    /// <summary>
    /// Local notifications on the device: shown at a time, by key, while the app is closed. A notification
    /// scheduled again under the same key replaces the one before. Nothing is shown while the player has them off.
    /// </summary>
    public partial interface INotificationService
    {
        /// <summary>The player's preference: off, nothing is scheduled and everything scheduled is cancelled.</summary>
        bool Enabled { get; }
        void SetEnabled(bool enabled);
        /// <summary>Asks the player's permission, where the platform asks and it has not been answered yet.</summary>
        void RequestPermission();
        /// <summary>Shows the title and text at the time (local), replacing what was scheduled under the key.</summary>
        void Schedule(string key, string title, string text, DateTime at);
        void Cancel(string key);
        public static partial class Commands { }
    }
}
