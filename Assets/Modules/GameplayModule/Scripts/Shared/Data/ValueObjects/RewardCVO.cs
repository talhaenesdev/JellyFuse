using System;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>What a login day or a quest gives: coins, and uses of a booster.</summary>
    [Serializable]
    public sealed class RewardCVO
    {
        [Min(0)] public int Coins;
        public BoosterType Booster;
        [Tooltip("Uses of Booster given; 0 gives none.")]
        [Min(0)] public int BoosterCount;
    }
}
