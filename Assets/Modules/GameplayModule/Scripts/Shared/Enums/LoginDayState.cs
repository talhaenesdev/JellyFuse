namespace Modules.GameplayModule.Shared.Enums
{
    /// <summary>How a day of the seven-day login reward stands.</summary>
    public enum LoginDayState
    {
        /// <summary>Its reward was taken.</summary>
        Claimed = 0,
        /// <summary>Today's reward, not taken yet.</summary>
        Today = 1,
        /// <summary>A day still to come.</summary>
        Locked = 2,
    }
}
