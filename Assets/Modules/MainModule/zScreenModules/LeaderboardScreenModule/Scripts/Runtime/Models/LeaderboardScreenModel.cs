using Modules.LeaderboardModule.Enums;

namespace Modules.MainModule.LeaderboardScreenModule.Models
{
    /// <summary>Which board the screen shows.</summary>
    internal sealed class LeaderboardScreenModel
    {
        public LeaderboardBoard Board { get; set; } = LeaderboardBoard.AllTime;
    }
}
