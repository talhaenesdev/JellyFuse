using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.WheelScreenModule.Models
{
    /// <summary>The last daily events announced, so the wheel opens filled.</summary>
    internal sealed class WheelModel
    {
        public EventsRVO Events { get; set; } = new();
    }
}
