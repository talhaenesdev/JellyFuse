using System.Threading.Tasks;
using Modules.LeaderboardModule.Data.ValueObjects;
using Modules.LeaderboardModule.Enums;

namespace Modules.LeaderboardModule.Services
{
    /// <summary>
    /// The Leaderboard Cloud Code module, from the game: a won level goes up and is scored on the server, and a board
    /// comes back. Both answer null when the player is offline or not signed in; nothing is retried.
    /// </summary>
    public interface ILeaderboardService
    {
        /// <summary>Sends a won level to be scored. <paramref name="difficulty"/> is 0 normal, 1 hard, 2 super hard.</summary>
        Task<LevelScoreVO> SubmitLevelAsync(int level, int difficulty, int secondsTotal, int secondsLeft, bool extraTime);
        /// <summary>The top <paramref name="limit"/> rows of a board and the player's own.</summary>
        Task<LeaderboardPageVO> GetBoardAsync(LeaderboardBoard board, int limit);
        /// <summary>The name, icon and frame every board shows for this player; false when it did not reach the server.</summary>
        Task<bool> SetProfileAsync(string name, string iconId, string frameId);
    }
}
