#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;
using Random = UnityEngine.Random;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Editor-only cheat: a door the board can finish is charged at once, the way a long combo charges one.</summary>
    internal sealed class ForceComboStrikeCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplayInternalSignals _internal { get; set; }

        private readonly List<int> _doors = new();

        public override void Execute()
        {
            if (_model.Snapshot().Status != PuzzleStatus.Playing) return;
            _doors.Clear();
            for (int door = 0; door < _model.DoorCount; door++) if (_model.DoorJellies(door).Count > 0) _doors.Add(door);
            if (_doors.Count == 0)
            {
                FlowLogger.LogWarning("ForceComboStrikeCommand - no door's shape can be made from the board now.");
                return;
            }
            _internal.ComboStrike.Dispatch(_doors[Random.Range(0, _doors.Count)]);
        }
    }
}
#endif
