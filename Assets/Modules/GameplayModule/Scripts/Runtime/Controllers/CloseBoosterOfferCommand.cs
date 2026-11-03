using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The purchase popup closed: if it showed a booster the offer is over, and the steps after this run the clock again.</summary>
    internal class CloseBoosterOfferCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }

        public override void Execute()
        {
            if (!_model.OfferedBooster.HasValue)
            {
                Retain();
                Stop();
                return;
            }
            _model.OfferedBooster = null;
        }
    }
}
