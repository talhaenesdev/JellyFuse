using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Does what the booster does on the board or the clock. The target is the door for a rocket and
    /// the colour for a UFO. When nothing could be done, nothing is paid: the sequence stops.
    /// </summary>
    internal class ApplyBoosterCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private BoosterType _type { get; set; }
        [SignalParam] private int _target { get; set; }

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_Boosters>();
            bool done = _type switch
            {
                BoosterType.Rocket => _model.ClearDoor(_target, config.RocketPreferBigPieces, config.RocketWithoutEnoughJellies),
                BoosterType.FreezeTime => _model.FreezeClock(config.FreezeSeconds),
                BoosterType.Ufo => _model.ClearColour((JellyColour)_target),
                _ => false,
            };
            if (!done)
            {
                Retain();
                Stop();
                return;
            }
            _signals.Outgoing.BoosterUsed.Dispatch(_type, _target);
            if (_type == BoosterType.FreezeTime) _signals.Outgoing.ClockFrozen.Dispatch(_model.ClockFreezeSeconds);
            else _signals.Outgoing.BoardChanged.Dispatch(new PuzzleTurnVO(new[] { _model.Snapshot() }));
        }
    }
}
