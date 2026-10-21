using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Models
{
    public interface IJellyPuzzleModel
    {
        float RemainingSeconds { get; }
        PuzzleStatus Status { get; }
        bool IsDragging { get; }
        bool IsTicking { get; set; }
        bool IsHooked { get; set; }
        /// <summary>Position in CD_JellyLevels of the level being played; -1 until the first start.</summary>
        int LevelIndex { get; set; }
        /// <summary>The pull reach of a door with none of its own (CD_JellyLevels.DefaultPullReach), read at every drag so a change applies at once.</summary>
        int DefaultPullReach { get; set; }
        /// <summary>How many steps back a jelly let go of with nothing to fuse returns to the last spot of this drag where it would have fused well (CD_JellyLevels.ReleaseFuseLookBack).</summary>
        int ReleaseFuseLookBack { get; set; }
        /// <summary>On: a held jelly finds its way round what is in the way. Off: it heads straight for the finger and stops against what blocks it.</summary>
        bool PathFinding { get; set; }
        /// <summary>On: a fuse must build on the biggest jelly of its colour that fits the waiting door. Off: any fuse that fits a waiting door goes.</summary>
        bool BiggestFuseFirst { get; set; }
        /// <summary>The win of the level being played has been paid; cleared when a level starts.</summary>
        bool WinPaid { get; set; }
        /// <summary>The loss of the level being played has cost its life; cleared when a level starts.</summary>
        bool LifeTaken { get; set; }
        /// <summary>A level is on screen: set when one starts, cleared when the gameplay screen closes.</summary>
        bool InPlay { get; set; }
        /// <summary>The clock has started: it waits for the level's first move.</summary>
        bool ClockStarted { get; set; }
        void Start(JellyLevelCVO level);
        bool Tick(float seconds);
        PuzzleSnapshotRVO Snapshot();
        PuzzleTurnVO Drop(Vector2Int cell, Vector2Int targetCell);
        PuzzleTurnVO Drag(Vector2Int cell, Vector2Int targetCell, bool released);
        bool Undo();
        /// <summary>Seconds the clock still stands still for; 0 while it runs.</summary>
        float ClockFreezeSeconds { get; }
        /// <summary>Stops the clock for this many seconds, on top of any freeze still running.</summary>
        bool FreezeClock(float seconds);
        /// <summary>
        /// Rocket: the jellies that would make the door's waiting jelly leave the board, and the door
        /// moves on to its next request. The jellies are picked by size - biggest first when
        /// <paramref name="biggestFirst"/> - so their cells add up to the waiting shape. Without an
        /// exact set, <paramref name="withoutEnough"/> takes the largest set that stays within it.
        /// False when the door waits for nothing, is frozen, or nothing could be taken.
        /// </summary>
        bool ClearDoor(int door, bool biggestFirst, bool withoutEnough);
        /// <summary>UFO: every jelly of the colour goes, and every door request for it is skipped. False when a frozen jelly is of the colour, or a frozen door still waits for it.</summary>
        bool ClearColour(JellyColour colour);
#if UNITY_EDITOR
        /// <summary>Editor-only cheat: ends the level now as won, or as lost with the clock at zero.</summary>
        void ForceResult(PuzzleStatus status);
#endif
    }
}
