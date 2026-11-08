using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.ValueObjects;
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
            else if (SlidIntoWall(before, after)) _audio.Play(AudioCue.Bump);
        }

        /// <summary>
        /// A jelly moved and now stands with its leading edge against a wall, a hole or the board's edge - the
        /// same moments the board sparks at.
        /// </summary>
        private static bool SlidIntoWall(PuzzleSnapshotRVO before, PuzzleSnapshotRVO after)
        {
            var level = after.Level;
            foreach (var piece in after.Pieces)
            {
                JellyPieceVO old = null;
                foreach (var candidate in before.Pieces) if (candidate.Id == piece.Id) { old = candidate; break; }
                if (old == null || old.Cells.Count != piece.Cells.Count) continue;
                var shift = Centre(piece.Cells) - Centre(old.Cells);
                if (shift.sqrMagnitude < .25f) continue;
                var direction = Mathf.Abs(shift.x) > Mathf.Abs(shift.y)
                    ? new Vector2Int((int)Mathf.Sign(shift.x), 0)
                    : new Vector2Int(0, (int)Mathf.Sign(shift.y));
                foreach (var cell in piece.Cells)
                {
                    var next = cell + direction;
                    if (Has(piece.Cells, next)) continue;
                    bool outside = next.x < 0 || next.y < 0 || next.x >= level.Width || next.y >= level.Height;
                    if (outside || Has(level.Walls, next) || Has(level.Holes, next)) return true;
                }
            }
            return false;
        }

        private static Vector2 Centre(IReadOnlyList<Vector2Int> cells)
        {
            var sum = Vector2.zero;
            foreach (var cell in cells) sum += cell;
            return sum / Mathf.Max(1, cells.Count);
        }

        private static bool Has(IReadOnlyList<Vector2Int> cells, Vector2Int cell)
        {
            foreach (var c in cells) if (c == cell) return true;
            return false;
        }

        private float ComboPitch() => Mathf.Pow(1.0595f, Mathf.Min(7, 2 * Mathf.Max(0, _feedback.Combo - 1)));
    }
}
