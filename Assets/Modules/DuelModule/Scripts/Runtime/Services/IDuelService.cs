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
    }
}
