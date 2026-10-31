using System;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>The local day a once-a-day product was last bought on, counted in days since 2000-01-01.</summary>
    [Serializable]
    public sealed class ShopPurchaseDaySVO
    {
        public string ProductId;
        public int Day;
    }
}
