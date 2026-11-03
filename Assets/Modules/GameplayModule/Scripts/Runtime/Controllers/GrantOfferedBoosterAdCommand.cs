using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The ad was watched: the offered booster gets today's free uses.</summary>
    internal class GrantOfferedBoosterAdCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }

        public override void Execute() => BoosterAdFunction.Grant(_shared, _model.OfferedBooster.Value);
    }
}
