using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The ad was watched: the player holds one more spin of the slot machine, to take when they choose.</summary>
    internal class GrantSlotSpinCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute() => _shared.GetScriptable<SD_JellyProgress>().SlotSpins++;
    }
}
