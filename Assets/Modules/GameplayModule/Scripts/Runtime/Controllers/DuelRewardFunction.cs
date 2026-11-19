using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The coins a won duel pays: CD_Events' duel reward, for the day's first paid wins, counted in SD_JellyProgress.</summary>
    internal static class DuelRewardFunction
    {
        public static int Pay(CD_Events events, SD_JellyProgress progress)
        {
            if (events == null || progress == null) return 0;
            int today = ShopDayFunction.Today();
            if (progress.DuelDay != today)
            {
                progress.DuelDay = today;
                progress.DuelWinsToday = 0;
            }
            if (progress.DuelWinsToday >= events.DuelRewardedWins) return 0;
            progress.DuelWinsToday++;
            return events.DuelRewardCoins;
        }
    }
}
