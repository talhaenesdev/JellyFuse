using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.DailyQuestsScreenModule.Models;
using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.DailyQuestsScreenModule.Controllers
{
    /// <summary>Keeps the events just announced for the next opening.</summary>
    internal class StoreQuestsCommand : Command
    {
        [Inject] private DailyQuestsModel _model { get; set; }
        [SignalParam] private EventsRVO _events { get; set; }

        public override void Execute() => _model.Events = _events;
    }
}
