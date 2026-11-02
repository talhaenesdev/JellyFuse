using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.LoginRewardsScreenModule.Models
{
    /// <summary>The last daily events announced, so the screen opens filled.</summary>
    internal sealed class LoginRewardsModel
    {
        public EventsRVO Events { get; set; } = new();
    }
}
