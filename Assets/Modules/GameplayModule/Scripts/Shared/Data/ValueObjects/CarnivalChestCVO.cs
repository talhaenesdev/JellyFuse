using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>One prize a carnival chest may hold, drawn by its weight against the chest's other prizes.</summary>
    [Serializable]
    public sealed class CarnivalPrizeCVO
    {
        [Min(1)] public int Weight = 1;
        [Min(0)] public int CoinsMin;
        [Min(0)] public int CoinsMax;
        [Min(0)] public int Rockets;
        [Min(0)] public int Freezes;
        [Min(0)] public int Ufos;
        [Min(0)] public int Lives;
    }

    /// <summary>A Color Carnival chest: the day's points it takes to open, and the prizes it may hold.</summary>
    [Serializable]
    public sealed class CarnivalChestCVO
    {
        [Min(1)] public int Points = 15;
        public List<CarnivalPrizeCVO> Prizes = new();
    }

    /// <summary>What a carnival chest gave: which chest, and the prize drawn from it.</summary>
    public sealed class CarnivalPrizeRVO
    {
        public int Chest;
        public int Coins;
        public int Rockets;
        public int Freezes;
        public int Ufos;
        public int Lives;
    }
}
