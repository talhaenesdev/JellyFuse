using System;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>The player's totals since they started, shown on the stats panel. Only counted up; nothing spends them.</summary>
    [Serializable]
    public sealed class JellyStatsSVO
    {
        [Tooltip("Jellies taken hold of on the board.")]
        [Min(0)] public long Taps;
        [Tooltip("Jellies fused into another.")]
        [Min(0)] public long Merges;
        [Tooltip("Jellies sent through their door.")]
        [Min(0)] public long Delivered;
        [Tooltip("Levels won.")]
        [Min(0)] public int LevelsWon;
        [Tooltip("Boosters used.")]
        [Min(0)] public int BoostersUsed;
        [Tooltip("The longest win streak reached.")]
        [Min(0)] public int BestWinStreak;
    }
}
