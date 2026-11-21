using System.Collections.Generic;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>
    /// A combo charging a door: the jellies that will fuse and fly into it, which door, the combo that earned it and
    /// how long they crackle before the board takes them.
    /// </summary>
    public sealed class ComboStrikeRVO
    {
        public readonly int Door;
        public readonly IReadOnlyList<int> PieceIds;
        public readonly int Combo;
        public readonly float Seconds;

        public ComboStrikeRVO(int door, IReadOnlyList<int> pieceIds, int combo, float seconds)
        {
            Door = door; PieceIds = pieceIds; Combo = combo; Seconds = seconds;
        }
    }
}
