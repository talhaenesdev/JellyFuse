using Modules.MainModule.EventsScreenModule.Signals;
using Modules.MainModule.EventsScreenModule.ViewsMediators;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;

namespace Modules.MainModule.EventsScreenModule.ViewsMediators
{
    public class EventsScreenMediator : IMediator
    {
		[Inject] private EventsScreenView _view { get; set; }
		[InjectSignal] private EventsScreenSignals _signals { get; set; }
        
        public virtual void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;
        }

        public virtual void OnRemove()
        {
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;
        }

        private void OnScreenShown(IScreenBody screen)
        {
            _view.BackClicked += OnBackClicked;
            _signals.Incoming.CloseEventsScreen.AddListener(OnCloseRequested);
            _view.DuelClicked += OnDuelClicked;
            _view.StarRushClaimClicked += OnStarRushClaim;
            _view.StarRushPlayClicked += OnBackClicked;
            _signals.Incoming.ApplyEvents.AddListener(_view.ApplyEvents);
            _signals.Outgoing.EventsRequested.Dispatch();
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.BackClicked -= OnBackClicked;
            _signals.Incoming.CloseEventsScreen.RemoveListener(OnCloseRequested);
            _view.DuelClicked -= OnDuelClicked;
            _view.StarRushClaimClicked -= OnStarRushClaim;
            _view.StarRushPlayClicked -= OnBackClicked;
            _signals.Incoming.ApplyEvents.RemoveListener(_view.ApplyEvents);
        }

        private void OnStarRushClaim()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.StarRushClaimClicked.Dispatch();
        }

        private void OnBackClicked()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _view.Hide();
            _signals.Outgoing.BackClicked.Dispatch();
        }

        private void OnCloseRequested()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _view.Hide();
        }

        private void OnDuelClicked()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _view.Hide();
            _signals.Outgoing.DuelClicked.Dispatch();
        }
    }
}
