using Modules.GameplayModule.DailyQuestsScreenModule.Signals;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace Modules.GameplayModule.DailyQuestsScreenModule.ViewsMediators
{
    public class DailyQuestsScreenMediator : IMediator
    {
		[Inject] private DailyQuestsScreenView _view { get; set; }
		[InjectSignal] private DailyQuestsScreenSignals _signals { get; set; }

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
            _view.ClaimClicked += OnClaimClicked;
            _view.CloseClicked += OnClose;
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _signals.Incoming.ApplyEvents.RemoveListener(_view.ShowEvents);
            _view.ClaimClicked -= OnClaimClicked;
            _view.CloseClicked -= OnClose;
        }

        private void OnClaimClicked(int index)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.ClaimClicked.Dispatch(index);
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
