using System.Collections.Generic;
using Modules.LeaderboardModule.Data.ValueObjects;
using Modules.LeaderboardModule.Enums;

namespace Modules.MainModule.LeaderboardScreenModule.Models
{
    /// <summary>Which board the screen shows, and the last copy of each board that came back.</summary>
    internal sealed class LeaderboardScreenModel
    {
        private readonly Dictionary<LeaderboardBoard, LeaderboardPageVO> _pages = new();

        public LeaderboardBoard Board { get; set; } = LeaderboardBoard.AllTime;

        /// <summary>The last copy of the board that came back; null before any has.</summary>
        public LeaderboardPageVO Cached(LeaderboardBoard board) => _pages.TryGetValue(board, out var page) ? page : null;

        /// <summary>Keeps a board that came back; nothing coming back leaves the last copy as it was.</summary>
        public void Remember(LeaderboardBoard board, LeaderboardPageVO page)
        {
            if (page != null) _pages[board] = page;
        }
    }
}
