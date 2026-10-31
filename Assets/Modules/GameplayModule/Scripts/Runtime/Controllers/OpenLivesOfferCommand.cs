using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.AdsModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Asks for the purchase popup to open on the lives offer.</summary>
    internal class OpenLivesOfferCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILivesModel _lives { get; set; }
        [Inject] private IAdsService _ads { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_Lives>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (config == null || progress == null) return;
            _signals.Outgoing.LivesOfferOpened.Dispatch(LivesOfferFunction.Build(_lives, progress, config, _ads.IsRewardedReady));
        }
    }
}
