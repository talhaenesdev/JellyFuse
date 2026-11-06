namespace Modules.LeaderboardModule.Data.ValueObjects
{
    /// <summary>What the server made of one won level.</summary>
    public sealed class LevelScoreVO
    {
        /// <summary>The points this win was worth.</summary>
        public int Points { get; set; }
        /// <summary>The level's best score so far, this one included.</summary>
        public int Best { get; set; }
        /// <summary>The player's all-time total: every level's best added up.</summary>
        public long Total { get; set; }
        /// <summary>This win beat the level's previous best.</summary>
        public bool Record { get; set; }
    }
}
