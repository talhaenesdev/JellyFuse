using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A level is played only with a life in hand. With none, lives are bought with coins when
    /// CD_Lives allows it and the purse holds enough; otherwise the player is told when the next
    /// life comes back, and the level does not start.
    /// </summary>
    internal class RequireLifeCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILivesModel _lives { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_Lives>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (config == null || progress == null || progress.Lives > 0) return;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (config.BuyWithCoins && progress.Coins >= config.CoinPrice)
            {
                progress.Coins -= config.CoinPrice;
                _lives.Add(progress, config, config.LivesPerPurchase, now);
                _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
                return;
            }
            var state = _lives.State(progress, config, now);
            _signals.Outgoing.LivesChanged.Dispatch(state);
            _signals.Outgoing.OutOfLives.Dispatch(state.SecondsToNext);
            Retain();
            Stop();
        }
    }
}
