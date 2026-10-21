using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Update;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    internal class StartPuzzleClockCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IUpdateProvider _updateProvider { get; set; }
        [InjectSignal] private GameplayInternalSignals _internalSignals { get; set; }

        public override void Execute()
        {
            if (!_model.IsTicking || _model.IsHooked) return;
            _model.IsHooked = true;
            var model = _model;
            var provider = _updateProvider;
            var tick = _internalSignals.Tick;
            Action onFrame = null;
            onFrame = () =>
            {
                if (!model.IsTicking)
                {
                    provider.RemoveUpdate(onFrame);
                    model.IsHooked = false;
                    return;
                }
                tick.Dispatch();
            };
            provider.AddUpdate(onFrame);
        }
    }
    internal class StopPuzzleClockCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        public override void Execute() => _model.IsTicking = false;
    }
    /// <summary>The gameplay screen closed: no level is on screen any more.</summary>
    internal class LeaveLevelCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        public override void Execute() => _model.InPlay = false;
    }
    /// <summary>Sets the clock running again after a pause, but only while the level is still being played.</summary>
    internal class ResumePuzzleClockCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        public override void Execute() => _model.IsTicking = _model.Status == PuzzleStatus.Playing && _model.ClockStarted;
    }
}


