using Modules.GameplayModule.Shared.Data.UnityObjects;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A short fingerprint of a level as it is authored: the same board gives the same text, any change to its jellies,
    /// doors, walls or clock gives another. A recorded run is raced only on the board it was played on.
    /// </summary>
    internal static class LevelFingerprintFunction
    {
        public static string Of(CD_JellyLevel level)
        {
            if (level == null) return string.Empty;
            string json = JsonUtility.ToJson(level);
            // FNV-1a over the authored values; the asset's name is not part of the board.
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                foreach (char c in json) { hash ^= c; hash *= 1099511628211UL; }
                return hash.ToString("x16");
            }
        }
    }
}
