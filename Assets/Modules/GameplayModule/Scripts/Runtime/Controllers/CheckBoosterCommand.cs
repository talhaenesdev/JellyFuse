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
    /// <summary>Lets a booster through only while a level is played, the booster is open, and a use is left or can be bought.</summary>
    internal class CheckBoosterCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [SignalParam] private BoosterType _type { get; set; }

        public override void Execute()
        {
            var booster = _shared.GetScriptable<CD_Boosters>()?.Get(_type);
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            bool usable = booster != null && booster.Enabled && progress != null && _model.Status == PuzzleStatus.Playing
                && _model.LevelIndex + 1 >= booster.UnlockLevel
                && (progress.Booster(_type).Count > 0 || progress.Coins >= booster.CoinPrice);
            if (usable) return;
            Retain();
            Stop();
        }
    }
}
