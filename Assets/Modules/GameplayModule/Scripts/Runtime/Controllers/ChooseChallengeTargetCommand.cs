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
            // The player's own progress, not whatever level was played last - a duel's level is not the run's.
            int won = levels == null ? 0 : levels.StartIndex(progress);
            if (_target == null || string.IsNullOrEmpty(_target.PlayerId) || events == null || won < events.DuelMinLevel)
            {
                FlowLogger.Log("ChooseChallengeTargetCommand - no challenge: no target, or too few levels won.");
                _signals.Outgoing.DuelUnavailable.Dispatch();
                Retain();
                Stop();
                return;
            }
            if (_duel.IsDeclined(_target.PlayerId, Time.realtimeSinceStartup))
            {
                FlowLogger.Log($"ChooseChallengeTargetCommand - {_target.Name} turned an invite down a moment ago.");
                _signals.Outgoing.DuelUnavailable.Dispatch();
                Retain();
                Stop();
                return;
            }
            // Any level both have won, picked at random above the easy first ones. A rival whose level is unknown or
            // below the floor does not drag the race down to level 1.
            int top = _target.Level > events.DuelLevelFloor ? Mathf.Min(won, _target.Level) : won;
            int index = levels.RandomPlayableBelow(top, events.DuelLevelFloor);
            // A rematch races somewhere new: the level just raced is drawn again only when there is nothing else.
            for (int retry = 0; retry < 4 && index >= 0 && index == _duel.LevelIndex; retry++)
                index = levels.RandomPlayableBelow(top, events.DuelLevelFloor);
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
