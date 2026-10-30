using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Records a level won or lost by the input just resolved, judged from the state captured before it
    /// the way the win sound is.
    /// </summary>
    public sealed class RecordPuzzleResultCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private PuzzleFeedbackModel _feedback { get; set; }
        [Inject] private IAnalyticsService _analytics { get; set; }

        public override void Execute()
        {
            var before = _feedback.Before;
            var after = _model.Snapshot();
            if (before == null || before.Level == null || before.Level != after.Level || before.Status != PuzzleStatus.Playing) return;
            if (after.Status == PuzzleStatus.Playing) return;

            _analytics.Record(after.Status == PuzzleStatus.Won ? "level_complete" : "level_fail", new Dictionary<string, object>
            {
                {"level", _model.LevelIndex + 1},
                {"moves", after.Moves},
                {"secondsLeft", (int) _model.RemainingSeconds},
                {"reason", after.Status == PuzzleStatus.Won ? "won" : "stuck"}
            });
        }
    }
}
