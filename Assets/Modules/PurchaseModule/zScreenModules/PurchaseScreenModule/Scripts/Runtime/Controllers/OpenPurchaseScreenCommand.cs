using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.LocalizationModule.Services;
using Modules.PurchaseModule.PurchaseScreenModule.ViewsMediators;
using Modules.PurchaseModule.Shared.Data.ValueObjects;

namespace Modules.PurchaseModule.PurchaseScreenModule.Controllers
{
    /// <summary>Opens the purchase popup on the offer carried.</summary>
    internal sealed class OpenPurchaseScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        // A Mediator injects nothing but its View, so the popup gets its texts from here.
        [Inject] private ILocalizationService _localization { get; set; }
        [SignalParam] private PurchaseOfferVO _offer { get; set; }

        /// <summary>Three ways out - opened, came back null, threw - and every one resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            var offer = _offer;
            try
            {
                PurchaseScreenView screen = await _screenService.Open<PurchaseScreenView>().Show<PurchaseScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenPurchaseScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }
                screen.SetLocalizer(_localization.Get);
                screen.ShowOffer(offer);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenPurchaseScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
