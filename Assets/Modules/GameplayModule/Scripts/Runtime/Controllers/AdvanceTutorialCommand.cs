using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A drag during a tutorial: its step is done when the jelly got where it was led, fused, or went out
    /// through a door - which can happen before the finger is lifted, and the screen then sends no release.
    /// Let go of short of that, it is shown again from where it is.
    /// </summary>
    internal class AdvanceTutorialCommand : Command
    {
        [Inject] private IJellyPuzzleModel _puzzle { get; set; }
        [Inject] private IJellyTutorialModel _tutorial { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private bool _released { get; set; }

        public override void Execute()
        {
            if (!_tutorial.Active) return;
            var board = _puzzle.Snapshot();
            // Held, a step is over only when its jelly went out through a door: reaching the end of the route
            // still waits for the release, so the jelly cannot be carried on along the next step.
            bool done = _released ? JellyTutorialFunction.StepDone(_tutorial, board) : JellyTutorialFunction.Jelly(_tutorial, board, out _) == null;
            if (!done && !_released) return;
            if (done)
            {
                _tutorial.Next();
                JellyTutorialFunction.FindJelly(_tutorial, board);
            }
            _signals.Outgoing.TutorialChanged.Dispatch(JellyTutorialFunction.Show(_tutorial, board));
        }
    }
}
