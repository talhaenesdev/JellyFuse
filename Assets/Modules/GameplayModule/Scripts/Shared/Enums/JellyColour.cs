namespace Modules.GameplayModule.Shared.Enums
{
    // The palette colours. 0..2 keep the IDs saved levels already use; the rest start at 100 so
    // they never meet a level's own Extra colours, which take 3..99 (CD_JellyLevel.ExtraColours).
    public enum JellyColour
    {
        Pink = 0, Blue = 1, Yellow = 2,
        Red = 100, Orange = 101, Green = 102, Mint = 103, Navy = 104, Purple = 105, Gray = 106, Black = 107,
    }
    public enum PuzzleStatus { Playing = 0, Won = 1, Lost = 2 }
    /// <summary>The side of the board a door sits on.</summary>
    public enum JellySide { Bottom = 0, Top = 1, Left = 2, Right = 3 }
    /// <summary>The one way an arrow-block jelly can move.</summary>
    public enum JellyAxis { Horizontal = 0, Vertical = 1 }
}
