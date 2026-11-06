using System.Collections.Generic;

namespace Modules.LeaderboardModule.Data.ValueObjects
{
    /// <summary>The top of a board and the asking player's own row, null when they have no score on it yet.</summary>
    public sealed class LeaderboardPageVO
    {
        public List<LeaderboardRowVO> Top { get; set; } = new();
        public LeaderboardRowVO Me { get; set; }
    }
}
