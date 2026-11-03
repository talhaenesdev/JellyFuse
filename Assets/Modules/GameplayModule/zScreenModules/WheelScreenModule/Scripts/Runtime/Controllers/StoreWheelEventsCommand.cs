using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.WheelScreenModule.Models;

namespace Modules.GameplayModule.WheelScreenModule.Controllers
{
    /// <summary>Keeps the events just announced for the next opening.</summary>
    internal class StoreWheelEventsCommand : Command
    {
        [Inject] private WheelModel _model { get; set; }
        [SignalParam] private EventsRVO _events { get; set; }

        public override void Execute() => _model.Events = _events;
    }
}
