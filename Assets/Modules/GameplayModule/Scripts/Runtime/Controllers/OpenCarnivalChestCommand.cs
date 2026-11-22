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
    /// <summary>The first carnival chest reached and not yet opened is opened: a prize drawn from it is paid and shown.</summary>
    internal sealed class OpenCarnivalChestCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILivesModel _lives { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var config = _shared.GetScriptable<CD_Events>();
            var chest = EventsFunction.CarnivalReady(progress, config);
            if (chest == null)
            {
                Retain();
                Stop();
                return;
            }
            var prize = EventsFunction.DrawCarnivalPrize(chest, progress.CarnivalOpened);
            progress.CarnivalOpened++;
            if (prize.Coins > 0)
            {
                progress.Coins += prize.Coins;
                _signals.Outgoing.CoinsEarned.Dispatch(prize.Coins);
            }
            if (prize.Rockets > 0) progress.Booster(BoosterType.Rocket).Count += prize.Rockets;
            if (prize.Freezes > 0) progress.Booster(BoosterType.FreezeTime).Count += prize.Freezes;
            if (prize.Ufos > 0) progress.Booster(BoosterType.Ufo).Count += prize.Ufos;
            var livesConfig = _shared.GetScriptable<CD_Lives>();
            if (prize.Lives > 0 && livesConfig != null) _lives.Add(progress, livesConfig, prize.Lives, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
            _signals.Outgoing.CarnivalChestOpened.Dispatch(prize);
            FlowLogger.Log($"OpenCarnivalChestCommand - chest {prize.Chest + 1} gave {prize.Coins} coins.");
        }
    }
}
