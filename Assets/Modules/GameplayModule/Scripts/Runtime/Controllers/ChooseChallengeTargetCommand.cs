using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Signals;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A player on the leaderboard was picked to be challenged: a level both have won is chosen, and this player plays it
    /// first; the cleared run is what the challenged player will race. Too few levels won, and the flow stops.
    /// </summary>
    internal class ChooseChallengeTargetCommand : Command
    {
        /// <summary>How far below the lower of the two players' levels the challenge level may be.</summary>
        private const int SPREAD = 15;

        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private DuelTargetVO _target { get; set; }

        public override void Execute()
        {
            var levels = _shared.GetScriptable<CD_JellyLevels>();
            var events = _shared.GetScriptable<CD_Events>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            int won = levels == null ? 0 : _model.LevelIndex >= 0 ? _model.LevelIndex : levels.StartIndex(progress);
            if (_target == null || string.IsNullOrEmpty(_target.PlayerId) || events == null || won < events.DuelMinLevel)
            {
                FlowLogger.Log("ChooseChallengeTargetCommand - no challenge: no target, or too few levels won.");
                _signals.Outgoing.DuelUnavailable.Dispatch();
                Retain();
                Stop();
                return;
            }
            // Both have won it: the lower of the two levels, or a little under it.
            int top = _target.Level > 0 ? Mathf.Min(won, _target.Level) : won;
            int index = levels.PlayableFrom(Mathf.Max(0, top - 1 - Random.Range(0, SPREAD)));
            if (index < 0 || index >= won) index = levels.PlayableFrom(Mathf.Max(0, won - 1));
            if (index < 0)
            {
                _signals.Outgoing.DuelUnavailable.Dispatch();
                Retain();
                Stop();
                return;
            }
            _model.Daily = false;
            _duel.Choose(index, _target.Name, _target.Icon, null);
            _duel.SetChallengeTarget(_target.PlayerId);
            FlowLogger.Log($"ChooseChallengeTargetCommand - challenging {_target.Name} on level {index + 1}.");
        }
    }
}
