using System.Collections.Generic;

namespace Modules.DuelModule.Data.ValueObjects
{
    /// <summary>A challenge sent to this player: the sender's cleared run on a level, waiting to be raced.</summary>
    public sealed class DuelChallengeVO
    {
        public string Id { get; set; } = string.Empty;
        public string FromId { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string FromIcon { get; set; } = string.Empty;
        /// <summary>Counted from 1.</summary>
        public int Level { get; set; }
        public List<float> Exits { get; set; } = new();
        public float Seconds { get; set; }
        public long SentAt { get; set; }
    }

    /// <summary>How a challenge this player sent went.</summary>
    public sealed class ChallengeOutcomeVO
    {
        public string OpponentName { get; set; } = string.Empty;
        public string OpponentIcon { get; set; } = string.Empty;
        public int Level { get; set; }
        public bool Declined { get; set; }
        /// <summary>The challenged player's time; -1 when they declined or did not clear the board.</summary>
        public float OpponentSeconds { get; set; }
        public float Seconds { get; set; }
        public bool SenderWon { get; set; }
    }

    public sealed class ChallengeMailVO
    {
        public List<DuelChallengeVO> Challenges { get; set; } = new();
        public List<ChallengeOutcomeVO> Results { get; set; } = new();
    }
}
