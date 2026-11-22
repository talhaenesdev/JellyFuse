using System;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>One step of the Star Rush meter: the week's stars it takes, and what it pays.</summary>
    [Serializable]
    public sealed class StarRushMilestoneCVO
    {
        [Min(1)] public int Stars = 10;
        [Min(0)] public int Coins;
        [Min(0)] public int Rockets;
        [Min(0)] public int Freezes;
        [Min(0)] public int Ufos;
        [Min(0)] public int Lives;
    }
}
