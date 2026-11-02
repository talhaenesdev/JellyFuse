using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The jellies the input just fused, judged from the state captured before it, count toward today's merge quests.</summary>
    internal class CountMergesCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private PuzzleFeedbackModel _feedback { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var before = _feedback.Before;
            var after = _model.Snapshot();
            if (progress == null || before == null || before.Level == null || before.Level != after.Level) return;
            // A jelly into its door leaves the board too; only what is left of the drop is fusion.
            int fused = before.Pieces.Count - after.Pieces.Count - (after.Exited - before.Exited);
            EventsFunction.Advance(progress, QuestType.MergePieces, fused);
        }
    }
}
