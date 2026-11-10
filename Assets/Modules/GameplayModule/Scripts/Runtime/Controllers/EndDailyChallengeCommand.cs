using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The level was left: the next start plays the run's own level again, not the daily challenge.</summary>
    internal class EndDailyChallengeCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        public override void Execute() => _model.Daily = false;
    }
}
