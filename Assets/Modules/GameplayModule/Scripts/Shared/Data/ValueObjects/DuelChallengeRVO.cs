namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>A player picked on the leaderboard to be sent a challenge.</summary>
    public sealed class DuelTargetVO
    {
        public string PlayerId { get; }
        public string Name { get; }
        public string Icon { get; }
        /// <summary>The highest level they have won; 0 when unknown.</summary>
        public int Level { get; }

        public DuelTargetVO(string playerId, string name, string icon, int level)
        {
            PlayerId = playerId;
            Name = name;
            Icon = icon;
            Level = level;
        }
    }

    /// <summary>A challenge waiting for this player.</summary>
    public sealed class ChallengeInviteRVO
    {
        public string Id { get; }
        public string FromName { get; }
        public string FromIcon { get; }
        /// <summary>Counted from 1.</summary>
        public int Level { get; }

        public ChallengeInviteRVO(string id, string fromName, string fromIcon, int level)
        {
            Id = id;
            FromName = fromName;
            FromIcon = fromIcon;
            Level = level;
        }
    }

    /// <summary>How a challenge this player sent went.</summary>
    public sealed class ChallengeOutcomeRVO
    {
        public string OpponentName { get; }
        public string OpponentIcon { get; }
        public bool Declined { get; }
        public bool SenderWon { get; }
        public float Seconds { get; }
        /// <summary>-1 when they declined or did not clear the board.</summary>
        public float OpponentSeconds { get; }

        public ChallengeOutcomeRVO(string opponentName, string opponentIcon, bool declined, bool senderWon, float seconds, float opponentSeconds)
        {
            OpponentName = opponentName;
            OpponentIcon = opponentIcon;
            Declined = declined;
            SenderWon = senderWon;
            Seconds = seconds;
            OpponentSeconds = opponentSeconds;
        }
    }
}
