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
            _view.StarRushPlayClicked += OnStarRushPlay;
            _view.CarnivalOpenClicked += OnCarnivalOpen;
            _view.CarnivalPlayClicked += OnCarnivalPlay;
            _view.SpeedRunStartClicked += OnSpeedRunStart;
            _view.SpeedRunAdClicked += OnSpeedRunAd;
            _signals.Incoming.ApplyCarnivalPrize.AddListener(_view.ShowCarnivalPrize);
            _signals.Incoming.ApplyEvents.AddListener(_view.ApplyEvents);
            _signals.Outgoing.EventsRequested.Dispatch();
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.BackClicked -= OnBackClicked;
            _signals.Incoming.CloseEventsScreen.RemoveListener(OnCloseRequested);
            _view.DuelClicked -= OnDuelClicked;
            _view.StarRushClaimClicked -= OnStarRushClaim;
            _view.StarRushPlayClicked -= OnStarRushPlay;
            _view.CarnivalOpenClicked -= OnCarnivalOpen;
            _view.CarnivalPlayClicked -= OnCarnivalPlay;
            _view.SpeedRunStartClicked -= OnSpeedRunStart;
            _view.SpeedRunAdClicked -= OnSpeedRunAd;
            _signals.Incoming.ApplyCarnivalPrize.RemoveListener(_view.ShowCarnivalPrize);
            _signals.Incoming.ApplyEvents.RemoveListener(_view.ApplyEvents);
        }

        private void OnStarRushPlay()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.StarRushPlayClicked.Dispatch();
        }

        private void OnCarnivalPlay()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.CarnivalPlayClicked.Dispatch();
        }

        private void OnSpeedRunStart()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.SpeedRunStartClicked.Dispatch();
        }

        private void OnSpeedRunAd()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.SpeedRunAdClicked.Dispatch();
        }

        private void OnCarnivalOpen()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.CarnivalOpenClicked.Dispatch();
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
