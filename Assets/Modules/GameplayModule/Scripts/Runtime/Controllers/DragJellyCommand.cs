using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.DevToolsModule.Services;
using Modules.GameplayModule.Constants;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Signals;
using UnityEngine;
namespace Modules.GameplayModule.Controllers
{
    public class DragJellyCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IDevToolsService _devTools { get; set; }
        [Inject] private IJellyTutorialModel _tutorial { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam(0)] private Vector2Int _cell { get; set; }
        [SignalParam(1)] private Vector2Int _targetCell { get; set; }
        [SignalParam] private bool _released { get; set; }
        public override void Execute()
        {
            // Read at every drag, so a change in the inspector applies to the next release.
            if (_released)
            {
                var levels = _shared.GetScriptable<CD_JellyLevels>();
                _model.DefaultPullReach = levels.DefaultPullReach;
                _model.ReleaseFuseLookBack = levels.ReleaseFuseLookBack;
            }
            // The developer switch decides whether a held jelly finds its way round, or heads straight for the finger.
            _model.PathFinding = _devTools == null || _devTools.IsOn(GameplayDevToggles.PATH_FINDING);
            // The developer switch decides whether a door's jelly must grow from the biggest fitting jelly, or from any.
            _model.BiggestFuseFirst = _devTools == null || _devTools.IsOn(GameplayDevToggles.BIGGEST_FUSE_FIRST);
            var target = _targetCell;
            if (_tutorial.Active)
            {
                // A tutorial step: only its jelly is taken hold of, and it goes only along the step's route.
                if (!_model.IsDragging)
                {
                    var jelly = JellyTutorialFunction.Jelly(_tutorial, _model.Snapshot(), out var at);
                    bool held = false;
                    if (jelly != null) foreach (var c in jelly.Cells) held |= c == _cell;
                    if (!held) { _signals.Outgoing.BoardChanged.Dispatch(new PuzzleTurnVO(new[] { _model.Snapshot() })); return; }
                    _tutorial.GrabOffset = _cell - at;
                }
                target = JellyTutorialFunction.Constrain(_tutorial, target);
            }
            var turn = _model.Drag(_cell, target, _released);
            if (turn.Frames.Count > 0) _signals.Outgoing.BoardChanged.Dispatch(turn);
        }
    }
}
