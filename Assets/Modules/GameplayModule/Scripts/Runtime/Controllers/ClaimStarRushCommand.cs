using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The Star Rush step reached first and not yet paid is paid: its coins, boosters and lives. Nothing waits, nothing runs on.</summary>
    internal sealed class ClaimStarRushCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILivesModel _lives { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var config = _shared.GetScriptable<CD_Events>();
            var step = EventsFunction.StarRushReady(progress, config);
            if (step == null)
            {
                Retain();
                Stop();
                return;
            }
            progress.StarRushClaimed++;
            if (step.Coins > 0)
            {
                progress.Coins += step.Coins;
                _signals.Outgoing.CoinsEarned.Dispatch(step.Coins);
            }
            if (step.Rockets > 0) progress.Booster(BoosterType.Rocket).Count += step.Rockets;
            if (step.Freezes > 0) progress.Booster(BoosterType.FreezeTime).Count += step.Freezes;
            if (step.Ufos > 0) progress.Booster(BoosterType.Ufo).Count += step.Ufos;
            var livesConfig = _shared.GetScriptable<CD_Lives>();
            if (step.Lives > 0 && livesConfig != null) _lives.Add(progress, livesConfig, step.Lives, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
            FlowLogger.Log($"ClaimStarRushCommand - step {progress.StarRushClaimed} paid at {progress.StarRushStars} stars.");
        }
    }
}
