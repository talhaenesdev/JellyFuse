using System;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>One slice of the daily prize wheel: what it gives, and how often it comes up against the others.</summary>
    [Serializable]
    public sealed class WheelSliceCVO
    {
        public RewardCVO Reward = new();
        [Tooltip("How likely the slice is, against the others' weights: 2 comes up twice as often as 1. 0 never comes up.")]
        [Min(0)] public float Weight = 1;
    }
}
