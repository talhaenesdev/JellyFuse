using Modules.GameplayModule.Shared.Data.UnityObjects;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// The bot keeps duels close: a player who has been winning its races meets a faster bot, one who has been losing a
    /// slower one. The last bot results are kept in SD_JellyProgress and move the bot's share of the level's clock.
    /// </summary>
    internal static class DuelBotPaceFunction
    {
        /// <summary>A bot duel ended: filed, keeping only the window CD_Events looks at.</summary>
        public static void Record(CD_Events events, SD_JellyProgress progress, bool won)
        {
            if (events == null || progress == null) return;
            progress.DuelBotResults.Add(won ? 1 : 0);
            int extra = progress.DuelBotResults.Count - Mathf.Max(1, events.DuelAdaptWindow);
            if (extra > 0) progress.DuelBotResults.RemoveRange(0, extra);
        }

        /// <summary>
        /// How far the bot's clock share moves: below zero is faster. Few results count for little - the win rate is
        /// pulled toward the target as if two duels had gone exactly as hoped.
        /// </summary>
        public static float Shift(CD_Events events, SD_JellyProgress progress)
        {
            if (events == null || progress == null || progress.DuelBotResults.Count == 0) return 0;
            int wins = 0;
            foreach (int result in progress.DuelBotResults) wins += result > 0 ? 1 : 0;
            float rate = (wins + events.DuelTargetWinRate * 2f) / (progress.DuelBotResults.Count + 2f);
            float shift = (events.DuelTargetWinRate - rate) * events.DuelAdaptGain * 2f;
            return Mathf.Clamp(shift, -events.DuelAdaptMaxShift, events.DuelAdaptMaxShift);
        }
    }
}
