using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.DevToolsModule.DevToolsScreenModule.Signals;

namespace Modules.DevToolsModule.DevToolsScreenModule.ViewsMediators
{
    public class DevToolsScreenMediator : IMediator
    {
        [Inject] private DevToolsScreenView _view { get; set; }
        [InjectSignal] private DevToolsScreenSignals _signals { get; set; }
        [InjectSignal] private DevToolsScreenInternalSignals _internalSignals { get; set; }

        public void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;
        }

        public void OnRemove()
        {
            OnScreenHidden(null);
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;
        }

        private void OnScreenShown(IScreenBody screen)
        {
            _view.CloseClicked += OnClose;
            _view.SkipLevelClicked += OnSkipLevel;
            _view.ResetBoostersClicked += OnResetBoosters;
            _view.RefillLivesClicked += OnRefillLives;
            _view.ResetPlayerClicked += OnResetPlayer;
            _view.SlidersOpened += OnSlidersOpened;
            _view.SliderChanged += OnSliderChanged;
            _view.SlidersResetClicked += OnSlidersReset;
            _signals.Incoming.ShowSliders.AddListener(_view.ShowSliders);
            _view.ToggleRequested += OnToggleRequested;
            _internalSignals.ToggleChanged.AddListener(_view.ShowToggle);
            _view.PinSubmitted += OnPinSubmitted;
            _internalSignals.PinRejected.AddListener(_view.ShowPinRejected);
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.CloseClicked -= OnClose;
            _view.SkipLevelClicked -= OnSkipLevel;
            _view.ResetBoostersClicked -= OnResetBoosters;
            _view.RefillLivesClicked -= OnRefillLives;
            _view.ResetPlayerClicked -= OnResetPlayer;
            _view.SlidersOpened -= OnSlidersOpened;
            _view.SliderChanged -= OnSliderChanged;
            _view.SlidersResetClicked -= OnSlidersReset;
            _signals.Incoming.ShowSliders.RemoveListener(_view.ShowSliders);
            _view.ToggleRequested -= OnToggleRequested;
            _internalSignals.ToggleChanged.RemoveListener(_view.ShowToggle);
            _view.PinSubmitted -= OnPinSubmitted;
            _internalSignals.PinRejected.RemoveListener(_view.ShowPinRejected);
        }

        private void OnPinSubmitted(string pin)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _internalSignals.PinEntered.Dispatch(pin);
        }

        private void OnToggleRequested(string key, bool on)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _internalSignals.SetToggle.Dispatch(key, on);
        }

        private void OnSlidersOpened(string page)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.SlidersRequested.Dispatch(page);
        }
        // Not guarded: a slider dragged while the panel opens or closes still reaches its owner.
        private void OnSliderChanged(string key, float value) => _signals.Outgoing.SliderChanged.Dispatch(key, value);
        private void OnSlidersReset(string page)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.SlidersResetRequested.Dispatch(page);
        }

        private void OnResetPlayer()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.ResetPlayerRequested.Dispatch();
        }

        private void OnRefillLives()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.RefillLivesRequested.Dispatch();
        }

        private void OnResetBoosters()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.ResetBoostersRequested.Dispatch();
        }

        private void OnSkipLevel(int direction)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.SkipLevelRequested.Dispatch(direction);
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
