using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Lets the ad for a doubled win through only on a paid win not doubled yet.</summary>
    internal class CheckDoubleRewardCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_AdRewards>();
            if (config != null && config.WinRewardMultiplier > 1 && _model.Status == PuzzleStatus.Won && _model.WinPaid
                && !_model.RewardDoubled && _model.WinReward > 0) return;
            Retain();
            Stop();
        }
    }
}
