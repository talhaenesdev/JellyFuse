using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.SlotScreenModule.Models
{
    /// <summary>The last daily events announced, so the machine opens filled.</summary>
    internal sealed class SlotModel
    {
        public EventsRVO Events { get; set; } = new();
    }
}
