using FlowIoC.BaseModule.Controller;
using FlowIoC.ConsoleModule;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;
using UnityEngine;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    public class StartPuzzleCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        public override void Execute()
        {
            var levels = _shared.GetScriptable<CD_JellyLevels>();
            if (_model.LevelIndex < 0)
            {
                // The first start of a run carries on from the level the player was on last time.
                _model.LevelIndex = levels.StartIndex(_shared.GetScriptable<SD_JellyProgress>());
            }
            var level = levels.Get(_model.LevelIndex);
            if (level == null || !levels.IsPlayable(_model.LevelIndex))
            {
                string why = level == null ? "no playable level from the start level on" : level.name + ": " + string.Join("; ", level.Validate());
                FlowLogger.LogError($"StartPuzzleCommand - cannot start a level: {why}. Fix it in Floria/Gameplay/Levels.");
                Retain();
                Stop();
                return;
            }
            _model.Start(level.CreateLevel(levels.Palette));
            _signals.Outgoing.LevelChanged.Dispatch(_model.LevelIndex + 1, levels.HasNext(_model.LevelIndex));
            _signals.Outgoing.BoardChanged.Dispatch(new PuzzleTurnVO(new[] { _model.Snapshot() }));
            _signals.Outgoing.TimeChanged.Dispatch(_model.RemainingSeconds);
            _signals.Outgoing.ClockFrozen.Dispatch(0f);
        }
    }
}


