using System;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>One symbol on the slot machine's reels: what three of it in a row give, and how often three come up against the other outcomes.</summary>
    [Serializable]
    public sealed class SlotSymbolCVO
    {
        public RewardCVO Reward = new();
        [Tooltip("How likely three of this symbol are, against the other symbols' weights and the pair's and the miss's: 2 comes up twice as often as 1.")]
        [Min(0)] public float Weight = 1;
    }
}
