using System;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>A quest the day may pick: what it counts, how many, and what it gives.</summary>
    [Serializable]
    public sealed class QuestCVO
    {
        public QuestType Type;
        [Min(1)] public int Target = 1;
        public RewardCVO Reward = new();
    }
}
