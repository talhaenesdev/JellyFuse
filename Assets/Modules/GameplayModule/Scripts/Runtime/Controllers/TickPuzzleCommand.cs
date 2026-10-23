using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    public class TickPuzzleCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [InjectSignal] private GameplayInternalSignals _internalSignals { get; set; }
        
        public override void Execute()
        {
            int previousSecond = Mathf.CeilToInt(_model.RemainingSeconds);
            bool wasFrozen = _model.ClockFreezeSeconds > 0;
            // One frame never takes more than Unity's longest step: back from the background, the first frame's
            // real time is all the time the game was away, and the clock does not run while the player is gone.
            bool expired = _model.Tick(Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime));
            if (wasFrozen && _model.ClockFreezeSeconds <= 0)
            {
                _signals.Outgoing.ClockFrozen.Dispatch(0f);
                _internalSignals.ClockThawed.Dispatch();
            }
            if (Mathf.CeilToInt(_model.RemainingSeconds) != previousSecond)
                _signals.Outgoing.TimeChanged.Dispatch(_model.RemainingSeconds);
            if (expired)
            {
                _signals.Outgoing.BoardChanged.Dispatch(new PuzzleTurnVO(new[] { _model.Snapshot() }));
                _internalSignals.TimeExpired.Dispatch();
            }
        }
    }
}


