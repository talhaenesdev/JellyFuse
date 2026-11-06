using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;
using Modules.LeaderboardModule.Services;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Sends the level just won to the leaderboard to be scored, and announces the points when the server answers.
    /// The win does not wait for the server: the step ends at once, and an answer that never comes shows nothing.
    /// </summary>
    internal class SubmitLevelScoreCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILeaderboardService _leaderboard { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var level = _shared.GetScriptable<CD_JellyLevels>()?.Get(_model.LevelIndex);
            if (level == null) return;
            int difficulty = level.Difficulty switch { LevelDifficulty.SuperHard => 2, LevelDifficulty.Hard => 1, _ => 0 };
            // Read into locals: the command goes back to its pool before the server answers.
            var scored = _leaderboard.SubmitLevelAsync(_model.LevelIndex + 1, difficulty, Mathf.RoundToInt(level.Seconds),
                Mathf.FloorToInt(_model.RemainingSeconds), _model.TimeExtended);
            var announce = _signals.Outgoing.LevelScored;
            scored.ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result != null) announce.Dispatch(task.Result.Points, task.Result.Record);
            }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
