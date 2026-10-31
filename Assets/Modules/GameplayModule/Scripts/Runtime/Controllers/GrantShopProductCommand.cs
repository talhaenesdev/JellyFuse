using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Gives what CD_ShopProducts says a paid product gives. Removing ads is a flag, so granting it twice -
    /// the store announces owned products again at every launch - changes nothing.
    /// </summary>
    internal class GrantShopProductCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILivesModel _lives { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private string _productId { get; set; }

        public override void Execute()
        {
            var product = _shared.GetScriptable<CD_ShopProducts>()?.Find(_productId);
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var lives = _shared.GetScriptable<CD_Lives>();
            if (product == null || progress == null)
            {
                FlowLogger.LogError($"Paid product {_productId} has no reward in CD_ShopProducts.");
                Retain();
                Stop();
                return;
            }

            if (product.Coins > 0)
            {
                progress.Coins += product.Coins;
                _signals.Outgoing.CoinsEarned.Dispatch(product.Coins);
                _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
            }
            if (product.Lives > 0 && lives != null)
                _lives.Add(progress, lives, product.Lives, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            foreach (var booster in product.Boosters)
                if (booster != null && booster.Count > 0) progress.Booster(booster.Type).Count += booster.Count;
            if (product.RemovesAds) progress.NoAds = true;
            if (product.OncePerDay) MarkBoughtToday(progress);
        }

        private void MarkBoughtToday(SD_JellyProgress progress)
        {
            var bought = progress.ShopPurchaseDays.Find(day => day.ProductId == _productId);
            if (bought == null) progress.ShopPurchaseDays.Add(bought = new ShopPurchaseDaySVO { ProductId = _productId });
            bought.Day = ShopDayFunction.Today();
        }
    }
}
