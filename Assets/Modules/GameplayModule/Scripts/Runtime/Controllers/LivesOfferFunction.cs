using System;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Constants;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.PurchaseModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The lives offer as the purchase popup shows it, from CD_Lives and the player's progress.</summary>
    internal static class LivesOfferFunction
    {
        public static PurchaseOfferVO Build(ILivesModel lives, SD_JellyProgress progress, CD_Lives config)
        {
            var state = lives.State(progress, config, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            bool full = state.Lives >= state.MaxLives;
            return new PurchaseOfferVO
            {
                Key = GameplayOffers.LIVES,
                TitleKey = config.TitleKey, Title = "More Lives",
                Icon = config.OfferIcon,
                Amount = $"{state.Lives}/{state.MaxLives}",
                InfoKey = full ? config.FullKey : config.NextLifeKey, Info = full ? "Your lives are full" : "Next life in {0}",
                CountdownSeconds = full ? 0 : state.SecondsToNext,
                BuyKey = config.BuyKey, Buy = "Buy {0}",
                Price = config.CoinPrice,
                CanBuy = !full && progress.Coins >= config.CoinPrice,
            };
        }
    }
}
