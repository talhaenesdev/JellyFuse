using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.PauseScreenModule.Signals;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.PauseScreenModule.ViewsMediators
{
    public class PauseScreenMediator : IMediator
    {
        [Inject] private PauseScreenView _view { get; set; }
        [InjectSignal] private PauseScreenSignals _signals { get; set; }
        [InjectSignal] private PauseScreenInternalSignals _internalSignals { get; set; }

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
            _view.OptionClicked += OnOptionClicked;
            _view.ResumeClicked += OnResume;
            _view.QuitClicked += OnQuit;
            _internalSignals.OptionChanged.AddListener(_view.ShowOption);
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.OptionClicked -= OnOptionClicked;
            _view.ResumeClicked -= OnResume;
            _view.QuitClicked -= OnQuit;
            _internalSignals.OptionChanged.RemoveListener(_view.ShowOption);
        }

        private void OnOptionClicked(SettingsOption option)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _internalSignals.ToggleOption.Dispatch(option);
        }

        // Neither button decides anything: the panel closes and says which way the player went.
        private void OnResume()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _view.Hide();
            _signals.Outgoing.Resumed.Dispatch();
        }

        private void OnQuit()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _view.Hide();
            _signals.Outgoing.QuitClicked.Dispatch();
        }
    }
}
