using System;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>One booster as the game offers it: when it opens, what the player gets for free, what one more costs.</summary>
    [Serializable]
    public sealed class BoosterCVO
    {
        public BoosterType Type;
        [Tooltip("The info page shown the first time the booster opens (CD_InfoPopups). Empty: none.")]
        public string InfoKey;
        [Tooltip("Off: the booster is not offered at all.")]
        public bool Enabled = true;
        public Sprite Icon;
        [Tooltip("The level, counted from 1, from which the booster can be used.")]
        [Min(1)] public int UnlockLevel = 1;
        [Tooltip("Uses given once, the first time the booster opens.")]
        [Min(0)] public int FreeUses = 3;
        [Tooltip("Coins one more use costs when the player has none left.")]
        [Min(0)] public int CoinPrice = 100;
    }
}
