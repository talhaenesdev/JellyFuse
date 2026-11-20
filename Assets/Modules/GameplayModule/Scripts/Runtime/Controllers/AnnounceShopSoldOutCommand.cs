using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Announces the once-a-day products already bought today, which the shop shows as sold out until midnight.</summary>
    internal class AnnounceShopSoldOutCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var products = _shared.GetScriptable<CD_ShopProducts>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var soldOut = new List<string>();
            if (products != null && progress != null)
            {
                int today = ShopDayFunction.Today();
                foreach (var bought in progress.ShopPurchaseDays)
                {
                    var product = bought != null ? products.Find(bought.ProductId) : null;
                    if (product == null) continue;
                    if ((product.OncePerDay && bought.Day == today) || product.OncePerAccount) soldOut.Add(bought.ProductId);
                }
                // Ads already gone: there is nothing left to sell.
                if (progress.NoAds)
                    foreach (var product in products.Products)
                        if (product != null && product.RemovesAds && !soldOut.Contains(product.ProductId)) soldOut.Add(product.ProductId);
            }
            _signals.Outgoing.ShopSoldOutChanged.Dispatch(soldOut);
        }
    }
}
