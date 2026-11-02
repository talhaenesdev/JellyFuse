using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>A day of the seven-day login reward as the popup draws it.</summary>
    public sealed class LoginDayRVO
    {
        public RewardCVO Reward;
        public LoginDayState State;
    }
}
