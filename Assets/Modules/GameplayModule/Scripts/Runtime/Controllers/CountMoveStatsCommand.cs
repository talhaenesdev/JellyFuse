using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// The input just played, judged from the state captured before it, adds to the player's totals: a jelly taken
    /// hold of, the jellies fused and the jellies sent through their doors.
    /// </summary>
    internal class CountMoveStatsCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private PuzzleFeedbackModel _feedback { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var before = _feedback.Before;
            if (progress == null || before == null || before.Level == null) return;
            progress.Stats ??= new();
            if (!_feedback.WasDragging && _model.IsDragging) progress.Stats.Taps++;
            var after = _model.Snapshot();
            if (before.Level != after.Level) return;
            int delivered = after.Exited - before.Exited;
            int fused = before.Pieces.Count - after.Pieces.Count - delivered;
            if (delivered > 0) progress.Stats.Delivered += delivered;
            if (fused > 0) progress.Stats.Merges += fused;
        }
    }
}
