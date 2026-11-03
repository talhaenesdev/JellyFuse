using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.ScratchScreenModule.Signals;

namespace Modules.GameplayModule.ScratchScreenModule.ViewsMediators
{
    public class ScratchScreenMediator : IMediator
    {
        [Inject] private ScratchScreenView _view { get; set; }
        [InjectSignal] private ScratchScreenSignals _signals { get; set; }

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
            _signals.Incoming.CardDealt.AddListener(_view.LayCard);
            _view.CardClicked += OnCardClicked;
            _view.AdCardClicked += OnAdCardClicked;
            _view.CloseClicked += OnClose;
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _signals.Incoming.ApplyEvents.RemoveListener(_view.ShowEvents);
            _signals.Incoming.CardDealt.RemoveListener(_view.LayCard);
            _view.CardClicked -= OnCardClicked;
            _view.AdCardClicked -= OnAdCardClicked;
            _view.CloseClicked -= OnClose;
        }

        private void OnCardClicked()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.CardClicked.Dispatch();
        }

        private void OnAdCardClicked()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.AdCardClicked.Dispatch();
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
