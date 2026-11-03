using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Buys one use of the offered booster with its coin price, while the purse holds it.</summary>
    internal class BuyOfferedBoosterCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var booster = _model.OfferedBooster.HasValue ? _shared.GetScriptable<CD_Boosters>()?.Get(_model.OfferedBooster.Value) : null;
            if (booster == null || progress == null || progress.Coins < booster.CoinPrice)
            {
                Retain();
                Stop();
                return;
            }
            progress.Coins -= booster.CoinPrice;
            progress.Booster(booster.Type).Count++;
            _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
        }
    }
}
