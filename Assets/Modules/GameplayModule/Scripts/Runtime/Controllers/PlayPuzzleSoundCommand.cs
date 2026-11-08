using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// The sound of one resolved input, judged from the state captured before it (the same
    /// before/after the haptic feedback reads): a win or a loss, a jelly into its door, a fusion,
    /// or a jelly picked up.
    /// </summary>
    public sealed class PlayPuzzleSoundCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private PuzzleFeedbackModel _feedback { get; set; }
        [Inject] private IAudioService _audio { get; set; }

        public override void Execute()
        {
            var before = _feedback.Before;
            var after = _model.Snapshot();
            if (before == null || before.Level == null || before.Level != after.Level || before.Status != PuzzleStatus.Playing) return;
            if (after.Status == PuzzleStatus.Won) _audio.Play(AudioCue.Win);
            else if (after.Status == PuzzleStatus.Lost) _audio.Play(AudioCue.Lose);
            // In a combo each fuse or door sounds two semitones higher than the last, up to a fifth.
            else if (after.Exited > before.Exited) _audio.Play(AudioCue.Deliver, ComboPitch());
            else if (after.Pieces.Count < before.Pieces.Count) _audio.Play(AudioCue.Merge, ComboPitch());
            else if (!_feedback.WasDragging && _model.IsDragging) _audio.Play(AudioCue.Grab);
        }

        private float ComboPitch() => Mathf.Pow(1.0595f, Mathf.Min(7, 2 * Mathf.Max(0, _feedback.Combo - 1)));
    }
}
