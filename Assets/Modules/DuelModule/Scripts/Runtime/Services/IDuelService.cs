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
        Task<bool> SubmitAsync(int level, IReadOnlyList<float> exits, string fingerprint);
        /// <summary>Sends one player a challenge: this player's cleared run on a level.</summary>
        Task<bool> SendChallengeAsync(string targetId, int level, IReadOnlyList<float> exits);
        /// <summary>The challenges waiting for this player and the outcomes of the ones it sent; null when the server did not answer.</summary>
        Task<ChallengeMailVO> GetChallengesAsync();
        /// <summary>Answers a challenge: declined, or raced in <paramref name="seconds"/> (-1 when the board was not cleared).</summary>
        Task<bool> AnswerChallengeAsync(string challengeId, bool declined, float seconds);
        /// <summary>Signed in, so a call can reach the server.</summary>
        bool IsReady { get; }

        /// <summary>
        /// Listens for the live duel's messages to this player - invite, start, progress, over, declined - each handed to
        /// <paramref name="onMessage"/> as its type and JSON body, on the main thread. False when it could not listen.
        /// </summary>
        Task<bool> ListenAsync(System.Action<string, string> onMessage);
        /// <summary>Invites a player whose game is open to race a level now; the match id, or null.</summary>
        Task<string> InviteLiveAsync(string targetId, int level);
        /// <summary>Answers a live invite; yes starts the race on both games.</summary>
        Task<bool> AnswerLiveAsync(string matchId, bool accept);
        /// <summary>Withdraws an invite this player sent and that has not been answered.</summary>
        Task CancelLiveAsync(string matchId);
        /// <summary>Another jelly cleared in a live race: the other player's bar moves.</summary>
        Task ProgressLiveAsync(string matchId, int done, int total);
        /// <summary>This player's live race ended: cleared in <paramref name="seconds"/>, or -1 when not cleared or left.</summary>
        Task FinishLiveAsync(string matchId, float seconds);
    }
}
