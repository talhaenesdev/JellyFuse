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
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private ISpeedRunModel _run { get; set; }
        [Inject] private IEventPlayModel _event { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        public override void Execute()
        {
            var levels = _shared.GetScriptable<CD_JellyLevels>();
            if (_model.LevelIndex < 0)
            {
                // The first start of a run carries on from the level the player was on last time.
                _model.LevelIndex = levels.StartIndex(_shared.GetScriptable<SD_JellyProgress>());
            }
            // A duel plays the level it raced on, and leaves the run's own level where it was.
            if (_duel.Active)
            {
                var duelLevel = levels.Get(_duel.LevelIndex);
                if (duelLevel != null)
                {
                    _model.Start(duelLevel.CreateLevel(levels.Palette));
                    _signals.Outgoing.LevelChanged.Dispatch(_duel.LevelIndex + 1, false);
                    _signals.Outgoing.BoardChanged.Dispatch(new PuzzleTurnVO(new[] { _model.Snapshot() }));
                    _signals.Outgoing.TimeChanged.Dispatch(_model.RemainingSeconds);
                    _signals.Outgoing.ClockFrozen.Dispatch(0f);
                    return;
                }
                _duel.End();
            }
            // A Speed Run plays its own levels on its own clock; a run whose last level was lost is over.
            if (_run.Active && _model.Status == PuzzleStatus.Lost) _run.End();
            if (_run.Active)
            {
                var runLevel = levels.Get(_run.CurrentLevel);
                if (runLevel != null)
                {
                    _model.Start(runLevel.CreateLevel(levels.Palette));
                    _model.SetClock(_run.Seconds);
                    _signals.Outgoing.LevelChanged.Dispatch(_run.CurrentLevel + 1, false);
                    _signals.Outgoing.SpeedRunChanged.Dispatch(new SpeedRunRVO { Active = true, Step = _run.Step + 1, Total = _run.Levels.Count });
                    _signals.Outgoing.BoardChanged.Dispatch(new PuzzleTurnVO(new[] { _model.Snapshot() }));
                    _signals.Outgoing.TimeChanged.Dispatch(_model.RemainingSeconds);
                    _signals.Outgoing.ClockFrozen.Dispatch(0f);
                    return;
                }
                _run.End();
            }
            _signals.Outgoing.SpeedRunChanged.Dispatch(new SpeedRunRVO());
            // An event's level: one the player has won, played again for the event; Next picks another.
            if (_event.Active)
            {
                var eventLevel = levels.Get(_event.LevelIndex);
                if (eventLevel != null && levels.IsPlayable(_event.LevelIndex))
                {
                    _model.Start(eventLevel.CreateLevel(levels.Palette));
                    _signals.Outgoing.LevelChanged.Dispatch(_event.LevelIndex + 1, true);
                    _signals.Outgoing.BoardChanged.Dispatch(new PuzzleTurnVO(new[] { _model.Snapshot() }));
                    _signals.Outgoing.TimeChanged.Dispatch(_model.RemainingSeconds);
                    _signals.Outgoing.ClockFrozen.Dispatch(0f);
                    return;
                }
                _event.End();
            }
            // Today's daily challenge, when it was picked and still has a level; else the run's level.
            var level = _model.Daily ? EventsFunction.DailyChallengeLevel(_shared.GetScriptable<CD_Events>()) : null;
            if (level == null) _model.Daily = false;
            if (_model.Daily)
            {
                _model.Start(level.CreateLevel(levels.Palette));
                // Level 0 is the daily challenge: the screen names it so instead of a number.
                _signals.Outgoing.LevelChanged.Dispatch(0, true);
                _signals.Outgoing.BoardChanged.Dispatch(new PuzzleTurnVO(new[] { _model.Snapshot() }));
                _signals.Outgoing.TimeChanged.Dispatch(_model.RemainingSeconds);
                _signals.Outgoing.ClockFrozen.Dispatch(0f);
                return;
            }
            level = levels.Get(_model.LevelIndex);
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


