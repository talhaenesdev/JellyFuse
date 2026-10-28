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
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.BackClicked -= OnBackClicked;
        }

        private void OnBackClicked()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _view.Hide();
            _signals.Outgoing.BackClicked.Dispatch();
        }
    }
}
