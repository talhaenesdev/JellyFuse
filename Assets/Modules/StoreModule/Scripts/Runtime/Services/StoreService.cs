using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using Modules.StoreModule.Data.ValueObjects;
using Modules.StoreModule.Services.Sub;
using UnityEngine.Purchasing;

namespace Modules.StoreModule.Services
{
    public class StoreService : IStoreService
    {
        private StoreController _store;
        private Action<string> _onPaid;
        private Action<IReadOnlyDictionary<string, string>> _onPrices;
        private readonly HashSet<string> _consumable = new();
        private bool _ready;
        private readonly PurchaseVerifySubService _verify = new();
        public IReadOnlyDictionary<string, string> Prices { get; private set; }

        public async void Connect(IReadOnlyList<StoreProductCVO> products, Action<IReadOnlyDictionary<string, string>> onPrices, Action<string> onPaid)
        {
            if (_store != null || products == null || products.Count == 0) return;
            _onPrices = onPrices;
            _onPaid = onPaid;
            foreach (var product in products.Where(p => p.Consumable)) _consumable.Add(product.Id);

            _store = UnityIAPServices.StoreController();
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += failure => FlowLogger.LogWarning($"Store products not fetched: {failure.FailureReason}");
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchasesFetched += OnPurchasesFetched;
            _store.OnPurchaseFailed += order => FlowLogger.LogWarning($"Purchase failed: {order.FailureReason}");
            _store.OnPurchaseDeferred += _ => FlowLogger.Log("Purchase waits on the store (deferred payment)");
            _store.OnPurchasesFetchFailed += failure => FlowLogger.LogWarning($"Owned products not fetched: {failure.failureReason}");
            _store.OnStoreConnected += () => FlowLogger.Log("Store connected");
            _store.OnStoreDisconnected += failure => FlowLogger.LogWarning($"Store disconnected: {failure.message}");
            try
            {
                await _store.Connect();
            }
            catch (Exception exception)
            {
                FlowLogger.LogWarning($"Store did not connect: {exception.Message}");
                return;
            }
            _store.FetchProducts(products
                .Where(p => p != null && !string.IsNullOrEmpty(p.Id))
                .Select(p => new ProductDefinition(p.Id, p.Consumable ? ProductType.Consumable : ProductType.NonConsumable))
                .ToList());
        }

        public bool Buy(string productId)
        {
            var product = _ready ? _store.GetProductById(productId) : null;
            if (product == null || !product.availableToPurchase) return false;
            _store.PurchaseProduct(product);
            return true;
        }

        private void OnProductsFetched(List<Product> products)
        {
            _ready = true;
            Prices = products.Where(p => p.availableToPurchase)
                .ToDictionary(p => p.definition.id, p => p.metadata.localizedPriceString);
            _onPrices?.Invoke(Prices);
            _store.FetchPurchases();
        }

        // Paid for now, or left pending last time. Granted and confirmed only once the server agrees; an
        // order left unconfirmed is delivered again next launch, and one Google never sold is refunded by Play
        // after three days unconfirmed.
        private async void OnPurchasePending(PendingOrder order)
        {
            foreach (var item in order.CartOrdered.Items())
            {
                string id = item.Product.definition.id;
                var verdict = await VerifyOrder(id, order.Info, _consumable.Contains(id));
                if (verdict != PurchaseVerdict.Valid) return;
                _onPaid?.Invoke(id);
            }
            _store.ConfirmPurchase(order);
        }

        // The restore: what the player owns for ever is granted again.
        private async void OnPurchasesFetched(Orders orders)
        {
            foreach (var order in orders.ConfirmedOrders)
            foreach (var item in order.CartOrdered.Items())
            {
                string id = item.Product.definition.id;
                if (_consumable.Contains(id)) continue;
                if (await VerifyOrder(id, order.Info, false) == PurchaseVerdict.Valid) _onPaid?.Invoke(id);
            }
        }

        // Only Google Play has a server to ask; the Editor's fake store is trusted.
        private Task<PurchaseVerdict> VerifyOrder(string productId, IOrderInfo info, bool consumable)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _verify.Verify(productId, info?.Google?.PurchaseToken, consumable);
#else
            return Task.FromResult(PurchaseVerdict.Valid);
#endif
        }
    }
}
