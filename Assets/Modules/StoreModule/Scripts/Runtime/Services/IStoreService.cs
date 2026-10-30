using System;
using System.Collections.Generic;
using Modules.StoreModule.Data.ValueObjects;

namespace Modules.StoreModule.Services
{
    /// <summary>Unity IAP. Announces through the callbacks it was connected with.</summary>
    public interface IStoreService
    {
        /// <summary>
        /// Connects to the store and lists the products. <paramref name="onPrices"/> hears the localized prices,
        /// <paramref name="onPaid"/> every product paid for, now or by an order left pending last time.
        /// </summary>
        void Connect(IReadOnlyList<StoreProductCVO> products, Action<IReadOnlyDictionary<string, string>> onPrices, Action<string> onPaid);

        /// <summary>Opens the store's payment sheet for the product. False when the store is not ready or does not sell it.</summary>
        bool Buy(string productId);

        /// <summary>The prices the store last answered with; null until it has.</summary>
        IReadOnlyDictionary<string, string> Prices { get; }
    }
}
