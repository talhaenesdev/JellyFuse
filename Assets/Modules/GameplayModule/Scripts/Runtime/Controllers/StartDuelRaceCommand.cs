using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Signals;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The duel level is on the board: the race starts after the countdown, and the screen is told who it is against.</summary>
    internal class StartDuelRaceCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            if (!_duel.Active) return;
            float countdown = _duel.Live ? _duel.LiveCountdown : _shared.GetScriptable<CD_Events>()?.DuelCountdown ?? 3f;
            _duel.StartRace(Time.realtimeSinceStartup, countdown);
            _signals.Outgoing.DuelStarted.Dispatch(new DuelRaceRVO(_duel.OpponentName, _duel.OpponentIcon, _duel.OpponentExits,
                DuelProgressFunction.Total(_model.Snapshot()), countdown, _duel.LevelIndex + 1));
        }
    }
}
