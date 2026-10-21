using Modules.GameplayModule.Shared.Data.ValueObjects;
namespace Modules.GameplayModule.Models
{
    public sealed class PuzzleFeedbackModel
    {
        public PuzzleSnapshotRVO Before { get; private set; }
        public bool WasDragging { get; private set; }
        public void Capture(PuzzleSnapshotRVO before, bool wasDragging) { Before = before; WasDragging = wasDragging; }
    }
}
