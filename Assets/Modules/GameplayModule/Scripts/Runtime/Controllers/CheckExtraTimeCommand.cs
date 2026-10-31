using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Lets the ad for more time through only on a level lost to the clock, once a level.</summary>
    internal class CheckExtraTimeCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_AdRewards>();
            if (config != null && config.ExtraSeconds > 0 && _model.Status == PuzzleStatus.Lost && _model.RemainingSeconds <= 0
                && !_model.TimeExtended) return;
            Retain();
            Stop();
        }
    }
}
