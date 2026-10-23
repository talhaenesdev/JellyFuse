using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;
using System.Collections.Generic;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Tells the HUD how each booster stands: locked or open, the uses left, the price of one more.</summary>
    internal class AnnounceBoostersCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var boosters = _shared.GetScriptable<CD_Boosters>();
            if (boosters == null) return;
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            int coins = progress != null ? progress.Coins : 0;
            var states = new List<BoosterStateRVO>();
            foreach (var booster in boosters.Boosters)
            {
                if (booster == null || !booster.Enabled) continue;
                int count = progress != null ? progress.Booster(booster.Type).Count : 0;
                bool unlocked = _model.LevelIndex + 1 >= booster.UnlockLevel;
                states.Add(new BoosterStateRVO
                {
                    Type = booster.Type, Icon = booster.Icon, Unlocked = unlocked, UnlockLevel = booster.UnlockLevel,
                    Count = count, Price = booster.CoinPrice,
                    // A freeze does not stack: while the clock is frozen, Freeze Time waits until it runs again.
                    Usable = unlocked && progress != null && (count > 0 || coins >= booster.CoinPrice)
                        && !(booster.Type == BoosterType.FreezeTime && _model.ClockFreezeSeconds > 0),
                });
            }
            _signals.Outgoing.BoostersChanged.Dispatch(states);
        }
    }
}
