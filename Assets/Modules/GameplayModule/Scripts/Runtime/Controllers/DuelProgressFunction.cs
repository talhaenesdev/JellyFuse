using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>How far a board is in a race: the jellies its doors have taken, out of all they will take.</summary>
    internal static class DuelProgressFunction
    {
        public static int Total(PuzzleSnapshotRVO snapshot)
        {
            int total = 0;
            if (snapshot?.Level != null)
                foreach (var gate in snapshot.Level.Gates) total += gate.Stages.Count;
            return total < 1 ? 1 : total;
        }

        public static int Done(PuzzleSnapshotRVO snapshot)
        {
            int done = 0;
            if (snapshot?.DoorProgress != null)
                foreach (int taken in snapshot.DoorProgress) done += taken;
            return done;
        }
    }
}
