using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.SlotScreenModule.Models;

namespace Modules.GameplayModule.SlotScreenModule.Controllers
{
    /// <summary>Keeps the events just announced for the next opening.</summary>
    internal class StoreSlotEventsCommand : Command
    {
        [Inject] private SlotModel _model { get; set; }
        [SignalParam] private EventsRVO _events { get; set; }

        public override void Execute() => _model.Events = _events;
    }
}
