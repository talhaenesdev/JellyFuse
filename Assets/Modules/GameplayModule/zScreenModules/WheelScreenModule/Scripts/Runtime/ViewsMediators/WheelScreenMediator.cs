using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.WheelScreenModule.Signals;

namespace Modules.GameplayModule.WheelScreenModule.ViewsMediators
{
    public class WheelScreenMediator : IMediator
    {
        [Inject] private WheelScreenView _view { get; set; }
        [InjectSignal] private WheelScreenSignals _signals { get; set; }

        public virtual void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;
        }

        public virtual void OnRemove()
        {
            OnScreenHidden(null);
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;
        }

        private void OnScreenShown(IScreenBody screen)
        {
            _signals.Incoming.ApplyEvents.AddListener(_view.ShowEvents);
            _signals.Incoming.WheelSpun.AddListener(_view.SpinTo);
            _view.SpinClicked += OnSpinClicked;
            _view.AdSpinClicked += OnAdSpinClicked;
            _view.CloseClicked += OnClose;
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _signals.Incoming.ApplyEvents.RemoveListener(_view.ShowEvents);
            _signals.Incoming.WheelSpun.RemoveListener(_view.SpinTo);
            _view.SpinClicked -= OnSpinClicked;
            _view.AdSpinClicked -= OnAdSpinClicked;
            _view.CloseClicked -= OnClose;
        }

        private void OnSpinClicked()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.SpinClicked.Dispatch();
        }

        private void OnAdSpinClicked()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.AdSpinClicked.Dispatch();
        }

        // Closing decides nothing, so the Mediator hides the screen itself.
        private void OnClose()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _signals.Outgoing.Closed.Dispatch();
            _view.Hide();
        }
    }
}
