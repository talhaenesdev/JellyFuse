using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The ad was watched: the booster gets CD_AdRewards' free uses, and today's free booster is taken.</summary>
    internal class GrantBoosterAdCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [SignalParam] private BoosterType _type { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            progress.Booster(_type).Count += _shared.GetScriptable<CD_AdRewards>().DailyFreeBoosters;
            progress.FreeBoosterAdDay = ShopDayFunction.Today();
        }
    }
}
