using System;
using System.Collections.Generic;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>What one store product gives the player. Its id is the product's id in Play Console.</summary>
    [Serializable]
    public sealed class ShopProductCVO
    {
        public string ProductId;
        public int Coins;
        public int Lives;
        /// <summary>Uses added to each booster listed.</summary>
        public List<BoosterSVO> Boosters = new();
        public bool RemovesAds;
        /// <summary>Sold once a day: bought today, the shop shows it sold out until the player's midnight.</summary>
        public bool OncePerDay;
    }
}
