using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.ScratchScreenModule.Models;

namespace Modules.GameplayModule.ScratchScreenModule.Controllers
{
    /// <summary>Keeps the events just announced for the next opening.</summary>
    internal class StoreScratchEventsCommand : Command
    {
        [Inject] private ScratchModel _model { get; set; }
        [SignalParam] private EventsRVO _events { get; set; }

        public override void Execute() => _model.Events = _events;
    }
}
