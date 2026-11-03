using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Lets an ad spin of the prize wheel through only after today's free one, while today's ad spins last.</summary>
    internal class CheckWheelAdSpinCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute()
        {
            if (EventsFunction.WheelAdSpinsLeft(_shared.GetScriptable<SD_JellyProgress>(), _shared.GetScriptable<CD_Events>()) > 0) return;
            Retain();
            Stop();
        }
    }
}
