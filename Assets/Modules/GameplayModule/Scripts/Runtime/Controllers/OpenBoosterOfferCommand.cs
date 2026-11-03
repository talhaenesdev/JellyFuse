using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.AdsModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A booster with no use left was pressed in play: the purchase popup opens on it, and the level's clock waits
    /// until the popup closes. A booster with a use left, or no level in play, opens nothing.
    /// </summary>
    internal class OpenBoosterOfferCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IAdsService _ads { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private BoosterType _type { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var offer = _model.Status == PuzzleStatus.Playing && progress != null && progress.Booster(_type).Count <= 0
                ? BoosterOfferFunction.Build(_shared, _model, _type, _ads.IsRewardedReady) : null;
            if (offer == null)
            {
                Retain();
                Stop();
                return;
            }
            _model.OfferedBooster = _type;
            _model.IsTicking = false;
            _signals.Outgoing.BoosterOfferOpened.Dispatch(offer);
        }
    }
}
