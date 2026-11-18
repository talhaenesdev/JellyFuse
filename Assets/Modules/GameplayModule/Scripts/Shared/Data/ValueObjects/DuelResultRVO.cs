namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>How a duel ended.</summary>
    public sealed class DuelResultRVO
    {
        /// <summary>The player cleared the board before the opponent did.</summary>
        public bool Won { get; }
        /// <summary>Coins the win paid; 0 for a loss, or once the day's paid wins are used up.</summary>
        public int Coins { get; }
        /// <summary>The player's time, or -1 when they did not clear the board.</summary>
        public float Seconds { get; }
        public float OpponentSeconds { get; }
        /// <summary>A challenge was being set: the cleared run went to its player rather than racing anyone.</summary>
        public bool ChallengeSent { get; }
        /// <summary>Who the challenge went to, when one was sent.</summary>
        public string ChallengedName { get; }

        public DuelResultRVO(bool won, int coins, float seconds, float opponentSeconds, bool challengeSent = false, string challengedName = "")
        {
            Won = won;
            Coins = coins;
            Seconds = seconds;
            OpponentSeconds = opponentSeconds;
            ChallengeSent = challengeSent;
            ChallengedName = challengedName ?? string.Empty;
        }
    }
}
