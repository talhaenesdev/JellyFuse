namespace Modules.GameplayModule.Constants
{
    /// <summary>The notifications the game schedules: their keys, and when the reminders come.</summary>
    internal static class GameplayNotifications
    {
        public const string LIVES_FULL = "lives_full";
        public const string COME_BACK_SOON = "come_back_1d";
        public const string COME_BACK_LATER = "come_back_3d";
        /// <summary>Hours without play after which each reminder is shown.</summary>
        public const int SOON_HOURS = 24;
        public const int LATER_HOURS = 72;
    }
}
