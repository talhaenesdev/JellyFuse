using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Lets the offered booster's ad through only while today's free use is still to take.</summary>
    internal class CheckOfferedBoosterAdCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }

        public override void Execute()
        {
            if (_model.OfferedBooster.HasValue && BoosterAdFunction.Offered(_shared, _model, _model.OfferedBooster.Value)) return;
            Retain();
            Stop();
        }
    }
}
