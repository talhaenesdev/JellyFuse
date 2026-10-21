using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The level's clock starts with its first move: until a jelly has moved, no time is spent.</summary>
    internal class StartClockOnMoveCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IJellyTutorialModel _tutorial { get; set; }

        public override void Execute()
        {
            // A tutorial takes no time: the clock waits for the first move after it.
            if (_tutorial.Active || _model.ClockStarted || _model.Status != PuzzleStatus.Playing || _model.Snapshot().Moves == 0) return;
            _model.ClockStarted = true;
            _model.IsTicking = true;
        }
    }
}
