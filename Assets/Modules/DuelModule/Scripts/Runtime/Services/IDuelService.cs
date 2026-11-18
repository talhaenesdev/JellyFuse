using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.DuelModule.Data.ValueObjects;

namespace Modules.DuelModule.Services
{
    /// <summary>
    /// The ghost race on the server. Both calls answer null or false when the player is offline or not signed in;
    /// nothing is retried.
    /// </summary>
    public interface IDuelService
    {
        /// <summary>An opponent on a level up to <paramref name="maxLevel"/> (counted from 1).</summary>
        Task<DuelMatchVO> FindAsync(int maxLevel);
        /// <summary>Files a won run: the second, from GO, each jelly left the board.</summary>
        Task<bool> SubmitAsync(int level, IReadOnlyList<float> exits);
        /// <summary>Sends one player a challenge: this player's cleared run on a level.</summary>
        Task<bool> SendChallengeAsync(string targetId, int level, IReadOnlyList<float> exits);
        /// <summary>The challenges waiting for this player and the outcomes of the ones it sent; null when the server did not answer.</summary>
        Task<ChallengeMailVO> GetChallengesAsync();
        /// <summary>Answers a challenge: declined, or raced in <paramref name="seconds"/> (-1 when the board was not cleared).</summary>
        Task<bool> AnswerChallengeAsync(string challengeId, bool declined, float seconds);
        /// <summary>Signed in, so a call can reach the server.</summary>
        bool IsReady { get; }
    }
}
