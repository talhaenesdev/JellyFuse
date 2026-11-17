using Modules.GameplayModule.StatsScreenModule.Signals;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace Modules.GameplayModule.StatsScreenModule.ViewsMediators
{
    public class StatsScreenMediator : IMediator
    {
        [Inject] private StatsScreenView _view { get; set; }
        [InjectSignal] private StatsScreenSignals _signals { get; set; }

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

        private void OnScreenShown(IScreenBody screen) => _view.CloseClicked += OnClose;

        private void OnScreenHidden(IScreenBody screen) => _view.CloseClicked -= OnClose;

        // Closing decides nothing, so the Mediator hides the screen itself.
        private void OnClose()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _signals.Outgoing.Closed.Dispatch();
            _view.Hide();
        }
    }
}
