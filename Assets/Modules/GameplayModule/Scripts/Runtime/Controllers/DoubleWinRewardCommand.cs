using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The ad was watched: the win pays its coins again, up to CD_AdRewards' multiplier, once.</summary>
    internal class DoubleWinRewardCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplayInternalSignals _internalSignals { get; set; }

        public override void Execute()
        {
            _model.RewardDoubled = true;
            int multiplier = _shared.GetScriptable<CD_AdRewards>().WinRewardMultiplier;
            _internalSignals.PayWin.Dispatch(_model.WinReward * (multiplier - 1));
        }
    }
}
