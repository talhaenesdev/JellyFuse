using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The ad was watched: the player holds one more spin of the prize wheel, to take when they choose.</summary>
    internal class GrantWheelSpinCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute() => _shared.GetScriptable<SD_JellyProgress>().WheelSpins++;
    }
}
