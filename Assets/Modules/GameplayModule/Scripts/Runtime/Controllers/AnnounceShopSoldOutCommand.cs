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
                    if (bought != null && bought.Day == today && products.Find(bought.ProductId) is { OncePerDay: true })
                        soldOut.Add(bought.ProductId);
            }
            _signals.Outgoing.ShopSoldOutChanged.Dispatch(soldOut);
        }
    }
}
