using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Buys CD_Lives' purchase with coins, while lives are not full and the purse holds the price.</summary>
    internal class BuyLivesCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILivesModel _lives { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_Lives>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (config == null || progress == null || progress.Lives >= config.MaxLives || progress.Coins < config.CoinPrice)
            {
                Retain();
                Stop();
                return;
            }
            progress.Coins -= config.CoinPrice;
            _lives.Add(progress, config, config.LivesPerPurchase, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
        }
    }
}
