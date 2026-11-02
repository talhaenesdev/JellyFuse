using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.LoginRewardsScreenModule.Models;
using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.LoginRewardsScreenModule.Controllers
{
    /// <summary>Keeps the events just announced for the next opening.</summary>
    internal class StoreLoginRewardsCommand : Command
    {
        [Inject] private LoginRewardsModel _model { get; set; }
        [SignalParam] private EventsRVO _events { get; set; }

        public override void Execute() => _model.Events = _events;
    }
}
