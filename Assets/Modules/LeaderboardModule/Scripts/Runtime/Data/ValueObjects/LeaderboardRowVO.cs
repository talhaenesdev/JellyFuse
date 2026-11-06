namespace Modules.LeaderboardModule.Data.ValueObjects
{
    /// <summary>One player's line on a board.</summary>
    public sealed class LeaderboardRowVO
    {
        /// <summary>Counted from 1.</summary>
        public int Rank { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Score { get; set; }
        /// <summary>The row of the player asking.</summary>
        public bool IsMe { get; set; }
    }
}
