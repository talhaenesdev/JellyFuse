using System;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>One of today's quests in the player's save, copied from CD_Events so an edited config does not change a day already started.</summary>
    [Serializable]
    public sealed class QuestSVO
    {
        public QuestType Type;
        public int Target;
        public int Progress;
        public bool Claimed;
        public RewardCVO Reward = new();

        public bool IsDone => Progress >= Target;
        public bool IsClaimable => IsDone && !Claimed;
    }
}
