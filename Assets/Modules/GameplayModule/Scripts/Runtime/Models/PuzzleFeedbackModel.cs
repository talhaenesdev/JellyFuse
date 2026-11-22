using Modules.GameplayModule.Shared.Data.ValueObjects;
namespace Modules.GameplayModule.Models
{
    public sealed class PuzzleFeedbackModel
    {
        /// <summary>A fuse or a door within this many seconds of the last one carries the combo on.</summary>
        /// <summary>The window used when none is given.</summary>
        public const float DEFAULT_COMBO_WINDOW = 4f;

        public PuzzleSnapshotRVO Before { get; private set; }
        public bool WasDragging { get; private set; }
        /// <summary>Fuses and doors in a row, each within the combo window of the last; 0 when the last input scored nothing.</summary>
        public int Combo { get; private set; }
        private double _lastScored = double.MinValue;

        public void Capture(PuzzleSnapshotRVO before, bool wasDragging) { Before = before; WasDragging = wasDragging; }

        /// <summary>One more fuse or door at this time: the combo grows within the window, or starts again at 1.</summary>
        public void Scored(double now, float window = DEFAULT_COMBO_WINDOW)
        {
            Combo = now - _lastScored <= window ? Combo + 1 : 1;
            if (Combo <= 1) Struck = false;
            _lastScored = now;
        }

        /// <summary>An input that scored nothing leaves no combo to read; the window still runs from the last score.</summary>
        public void NotScored() { Combo = 0; Struck = false; }

        /// <summary>Whether this combo has already finished a door by itself; one chain finishes one.</summary>
        public bool Struck { get; private set; }
        public void MarkStruck() => Struck = true;
    }
}
