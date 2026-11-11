using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.MainModule.ConsentScreenModule.Signals;

namespace Modules.MainModule.ConsentScreenModule.ViewsMediators
{
    public class ConsentScreenMediator : IMediator
    {
        [Inject] private ConsentScreenView _view { get; set; }
        [InjectSignal] private ConsentScreenSignals _signals { get; set; }

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

        private void OnScreenShown(IScreenBody screen) => _view.Answered += OnAnswered;
        private void OnScreenHidden(IScreenBody screen) => _view.Answered -= OnAnswered;

        // Either answer closes the panel; what follows from it is decided where it lands.
        private void OnAnswered(bool accepted)
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _view.Hide();
            _signals.Outgoing.Answered.Dispatch(accepted);
        }
    }
}
