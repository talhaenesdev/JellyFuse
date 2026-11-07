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
        /// <summary>The profile icon the player picked, or empty when they never picked one.</summary>
        public string Icon { get; set; } = string.Empty;
        /// <summary>The profile frame the player picked, or empty.</summary>
        public string Frame { get; set; } = string.Empty;
    }
}
