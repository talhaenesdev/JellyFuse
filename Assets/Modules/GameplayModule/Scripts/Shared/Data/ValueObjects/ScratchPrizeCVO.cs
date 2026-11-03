using System;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>One prize on the scratch card: what three of it give, and how often a card wins it against the others and against no win.</summary>
    [Serializable]
    public sealed class ScratchPrizeCVO
    {
        public RewardCVO Reward = new();
        [Tooltip("How likely a card wins this prize, against the other prizes' weights and the miss's: 2 comes up twice as often as 1.")]
        [Min(0)] public float Weight = 1;
    }
}
