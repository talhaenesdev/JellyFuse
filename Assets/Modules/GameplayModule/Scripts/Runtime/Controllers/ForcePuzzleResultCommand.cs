#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Editor-only cheat: ends the level as won or lost at once, so the win and lose panels can be
    /// tested without playing it out. Compiled out of every build.
    /// </summary>
    internal class ForcePuzzleResultCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private PuzzleStatus _status { get; set; }

        public override void Execute()
        {
            FlowLogger.Log($"ForcePuzzleResultCommand - forcing {_status} (editor only).");
            _model.ForceResult(_status);
            _signals.Outgoing.BoardChanged.Dispatch(new PuzzleTurnVO(new[] { _model.Snapshot() }));
            _signals.Outgoing.TimeChanged.Dispatch(_model.RemainingSeconds);
        }
    }
}
#endif
