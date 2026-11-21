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
    /// <summary>The chosen door's jellies start to crackle: the board is told which ones, for how long.</summary>
    internal sealed class AnnounceComboStrikeCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private PuzzleFeedbackModel _feedback { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private int _door { get; set; }

        public override void Execute()
        {
            var pieces = _model.DoorJellies(_door);
            if (pieces.Count == 0)
            {
                Retain();
                Stop();
                return;
            }
            var config = _shared.GetScriptable<CD_Boosters>();
            _signals.Outgoing.ComboStruck.Dispatch(new ComboStrikeRVO(_door, pieces, _feedback.Combo, config.ComboStrikeSeconds));
        }
    }
}
