using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.PurchaseModule.PurchaseScreenModule.Signals;
using Modules.PurchaseModule.Shared.Data.ValueObjects;

namespace Modules.PurchaseModule.PurchaseScreenModule.ViewsMediators
{
    public class PurchaseScreenMediator : IMediator
    {
        [Inject] private PurchaseScreenView _view { get; set; }
        [InjectSignal] private PurchaseScreenSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;
        }

        public void OnRemove()
        {
            OnScreenHidden(_view);
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;
        }

        private void OnScreenShown(IScreenBody screen)
        {
            _view.BuyClicked += OnBuy;
            _view.CloseClicked += OnClose;
            _view.RefreshRequested += OnRefresh;
            _signals.Incoming.ApplyOffer.AddListener(OnApplyOffer);
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.BuyClicked -= OnBuy;
            _view.CloseClicked -= OnClose;
            _view.RefreshRequested -= OnRefresh;
            _signals.Incoming.ApplyOffer.RemoveListener(OnApplyOffer);
        }

        // An update for another offer than the one shown is not this popup's.
        private void OnApplyOffer(PurchaseOfferVO offer)
        {
            if (_view.Offer != null && _view.Offer.Key == offer.Key) _view.ShowOffer(offer);
        }

        private void OnBuy()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal) && _view.Offer != null) _signals.Outgoing.Bought(_view.Offer.Key).Dispatch();
        }

        private void OnRefresh()
        {
            if (_view.Offer != null) _signals.Outgoing.RefreshRequested(_view.Offer.Key).Dispatch();
        }

        // Closing decides nothing, so the Mediator hides the screen itself.
        private void OnClose()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _view.Hide();
            _signals.Outgoing.Closed.Dispatch();
        }
    }
}
