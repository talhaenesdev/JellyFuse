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
    /// <summary>
    /// Gives the free uses of every booster that is open at the level now played and has not been
    /// given them yet. The step after it writes SD_JellyProgress to disk.
    /// </summary>
    internal class GrantBoostersCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var boosters = _shared.GetScriptable<CD_Boosters>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (boosters == null || progress == null) return;
            foreach (var booster in boosters.Boosters)
            {
                if (booster == null || !booster.Enabled || _model.LevelIndex + 1 < booster.UnlockLevel) continue;
                var saved = progress.Booster(booster.Type);
                if (saved.Granted) continue;
                saved.Granted = true;
                saved.Count += booster.FreeUses;
                // The first time it opens, the player is told what it does.
                if (!string.IsNullOrEmpty(booster.InfoKey)) _signals.Outgoing.InfoRequested.Dispatch(booster.InfoKey);
            }
        }
    }
}
