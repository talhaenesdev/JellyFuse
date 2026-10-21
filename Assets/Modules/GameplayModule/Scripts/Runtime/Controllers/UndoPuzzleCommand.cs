using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Takes back the last move.</summary>
    public class UndoPuzzleCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        public override void Execute()
        {
            if (_model.Undo()) _signals.Outgoing.BoardChanged.Dispatch(new PuzzleTurnVO(new[] { _model.Snapshot() }));
        }
    }
}
