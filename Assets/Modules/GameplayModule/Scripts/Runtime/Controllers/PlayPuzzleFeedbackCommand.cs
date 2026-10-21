using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Enums;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Services;
namespace Modules.GameplayModule.Controllers
{
    public sealed class PlayPuzzleFeedbackCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private PuzzleFeedbackModel _feedback { get; set; }
        [Inject] private IHapticService _haptics { get; set; }
        public override void Execute()
        {
            var before = _feedback.Before;
            var after = _model.Snapshot();
            if (before == null || before.Level == null || before.Level != after.Level || before.Status != PuzzleStatus.Playing) return;
            // One effect per resolved input. A final exit is a win, not two overlapping effects.
            if (after.Status == PuzzleStatus.Won) _haptics.Play(HapticPreset.Success);
            else if (after.Status == PuzzleStatus.Lost) _haptics.Play(HapticPreset.Failure);
            // A jelly going into its door trembles for as long as it melts; the screen plays that (Melt).
            else if (after.Exited > before.Exited) return;
            else if (after.Pieces.Count < before.Pieces.Count) _haptics.Play(HapticPreset.Medium);
            else if (!_feedback.WasDragging && _model.IsDragging) _haptics.Play(HapticPreset.Selection);
        }
    }
}
