using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Takes one use of the booster; CheckBoosterCommand let it through only with one left. The step after it writes SD_JellyProgress to disk.</summary>
    internal class PayBoosterCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [SignalParam] private BoosterType _type { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            progress.Booster(_type).Count--;
        }
    }
}
