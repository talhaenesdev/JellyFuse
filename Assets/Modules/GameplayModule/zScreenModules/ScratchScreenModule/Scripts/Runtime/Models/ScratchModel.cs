using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.ScratchScreenModule.Models
{
    /// <summary>The last daily events announced, so the card opens filled.</summary>
    internal sealed class ScratchModel
    {
        public EventsRVO Events { get; set; } = new();
    }
}
