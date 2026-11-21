using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The charge lands: the door takes its shape from the board the way a rocket does, and the board is redrawn.</summary>
    internal sealed class StrikeComboDoorCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private int _door { get; set; }

        public override void Execute()
        {
            if (!_model.ClearDoor(_door, true, false))
            {
                FlowLogger.Log($"StrikeComboDoorCommand - door {_door} could no longer be finished.");
                Retain();
                Stop();
                return;
            }
            _signals.Outgoing.BoardChanged.Dispatch(new PuzzleTurnVO(new[] { _model.Snapshot() }));
        }
    }
}
