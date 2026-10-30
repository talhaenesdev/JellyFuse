using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Enums;
using Modules.ReviewModule.Services;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Asks for a rating when a player has just won one of the milestone levels - happy, and far enough
    /// in to judge the game. Play itself limits how often the sheet really appears.
    /// </summary>
    public sealed class RequestReviewCommand : Command
    {
        private readonly int[] _milestones = {10, 25, 50};

        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private PuzzleFeedbackModel _feedback { get; set; }
        [Inject] private IReviewService _review { get; set; }

        public override void Execute()
        {
            var before = _feedback.Before;
            if (before == null || before.Status != PuzzleStatus.Playing || _model.Status != PuzzleStatus.Won) return;
            if (System.Array.IndexOf(_milestones, _model.LevelIndex + 1) < 0) return;
            _review.Request();
        }
    }
}
