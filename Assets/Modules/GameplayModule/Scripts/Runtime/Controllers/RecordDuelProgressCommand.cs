using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Signals;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>After a move or a booster in a duel: every jelly that left the board is timed, and the race bar told.</summary>
    internal class RecordDuelProgressCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IDuelModel _duel { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            if (!_duel.Active || _duel.Finished) return;
            var snapshot = _model.Snapshot();
            int done = DuelProgressFunction.Done(snapshot);
            if (_duel.RecordExits(done, Time.realtimeSinceStartup))
                _signals.Outgoing.DuelProgress.Dispatch(Mathf.Clamp01(done / (float)DuelProgressFunction.Total(snapshot)));
        }
    }
}
