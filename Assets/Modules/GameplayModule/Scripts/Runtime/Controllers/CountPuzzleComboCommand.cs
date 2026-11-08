using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Counts the combo of the input just resolved, before the feedback that reads it: a fuse or a jelly into its
    /// door scores, each within the window of the last carrying the combo on.
    /// </summary>
    public sealed class CountPuzzleComboCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private PuzzleFeedbackModel _feedback { get; set; }

        public override void Execute()
        {
            var before = _feedback.Before;
            var after = _model.Snapshot();
            bool playing = before != null && before.Level != null && before.Level == after.Level && before.Status == PuzzleStatus.Playing;
            if (playing && (after.Exited > before.Exited || after.Pieces.Count < before.Pieces.Count)) _feedback.Scored(Time.realtimeSinceStartupAsDouble);
            else _feedback.NotScored();
        }
    }
}
