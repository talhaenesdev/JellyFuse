using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// After a scoring move: a combo long enough, not yet spent, picks a door at random whose waiting shape the
    /// board can make exactly, and that door finishes itself. One combo finishes one door.
    /// </summary>
    internal sealed class CheckComboStrikeCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private PuzzleFeedbackModel _feedback { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplayInternalSignals _internal { get; set; }

        private readonly List<int> _doors = new();

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_Boosters>();
            if (config == null || config.ComboStrikeAt <= 0 || _feedback.Struck || _feedback.Combo < config.ComboStrikeAt) return;
            if (_model.Snapshot().Status != PuzzleStatus.Playing) return;
            _doors.Clear();
            for (int door = 0; door < _model.DoorCount; door++) if (_model.DoorJellies(door).Count > 0) _doors.Add(door);
            if (_doors.Count == 0) return;
            _feedback.MarkStruck();
            int chosen = _doors[Random.Range(0, _doors.Count)];
            FlowLogger.Log($"CheckComboStrikeCommand - combo {_feedback.Combo} charges door {chosen}.");
            _internal.ComboStrike.Dispatch(chosen);
        }
    }
}
