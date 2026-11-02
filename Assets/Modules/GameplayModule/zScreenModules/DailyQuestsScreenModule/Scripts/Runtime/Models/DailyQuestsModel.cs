using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.DailyQuestsScreenModule.Models
{
    /// <summary>The last daily events announced, so the screen opens filled.</summary>
    internal sealed class DailyQuestsModel
    {
        public EventsRVO Events { get; set; } = new();
    }
}
