using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Gives today's prize wheel spins back - the free one and the ad ones - as on a new day. For the developer panel.</summary>
    internal class ResetWheelCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            progress.WheelFreeTaken = false;
            progress.WheelAdSpins = 0;
        }
    }
}
