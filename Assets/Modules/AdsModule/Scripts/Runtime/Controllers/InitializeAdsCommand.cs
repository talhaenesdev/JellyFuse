using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;

namespace Modules.AdsModule.Controllers
{
    internal class InitializeAdsCommand : Command
    {
        [Inject] private AdsModel _model { get; set; }
        [Inject] private IAdsService _ads { get; set; }

        public override void Execute()
        {
            if (_model.Config != null) _ads.Initialize(_model.Config.AndroidAppKey, _model.Config.AndroidRewardedAdUnitId);
        }
    }
}
