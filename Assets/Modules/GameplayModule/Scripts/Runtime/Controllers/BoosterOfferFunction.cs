using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Constants;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.PurchaseModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// One more use of a booster as the purchase popup shows it: its name and icon, the uses held, its coin price,
    /// and today's free use for an ad (CD_AdRewards) while that is still to take.
    /// </summary>
    internal static class BoosterOfferFunction
    {
        public static PurchaseOfferVO Build(ISharedDataModel shared, IJellyPuzzleModel model, BoosterType type, bool adReady)
        {
            var booster = shared.GetScriptable<CD_Boosters>()?.Get(type);
            var progress = shared.GetScriptable<SD_JellyProgress>();
            var ads = shared.GetScriptable<CD_AdRewards>();
            if (booster == null || progress == null) return null;
            int perAd = ads != null ? ads.DailyFreeBoosters : 0;
            return new PurchaseOfferVO
            {
                Key = GameplayOffers.BOOSTER,
                TitleKey = $"info.booster.{type.ToString().ToLowerInvariant()}.title", Title = type.ToString(),
                Icon = booster.Icon,
                Amount = progress.Booster(type).Count.ToString(),
                InfoKey = "booster.offer.info", Info = "Out of uses! Get one more.",
                BuyKey = "lives.buy", Buy = "Buy  {0}",
                Price = booster.CoinPrice,
                CanBuy = progress.Coins >= booster.CoinPrice,
                WatchKey = perAd > 0 ? "lives.watch_ad" : null, Watch = perAd > 0 ? "Watch Ad  +{0}" : null,
                WatchAmount = perAd,
                CanWatch = adReady && BoosterAdFunction.Offered(shared, model, type),
            };
        }
    }
}
