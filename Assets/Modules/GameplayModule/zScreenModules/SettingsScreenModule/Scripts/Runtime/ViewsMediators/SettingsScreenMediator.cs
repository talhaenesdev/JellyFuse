using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.SettingsScreenModule.Signals;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.SettingsScreenModule.ViewsMediators
{
    public class SettingsScreenMediator : IMediator
    {
        [Inject] private SettingsScreenView _view { get; set; }
        [InjectSignal] private SettingsScreenSignals _signals { get; set; }
        [InjectSignal] private SettingsScreenInternalSignals _internalSignals { get; set; }

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
            _view.LanguagePicked += OnLanguagePicked;
            _view.OptionClicked += OnOptionClicked;
            _internalSignals.OptionChanged.AddListener(_view.ShowOption);
            _view.CloseClicked += OnClose;
            _signals.Incoming.ApplyLanguage.AddListener(_view.ShowCurrent);
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.LanguagePicked -= OnLanguagePicked;
            _view.OptionClicked -= OnOptionClicked;
            _internalSignals.OptionChanged.RemoveListener(_view.ShowOption);
            _view.CloseClicked -= OnClose;
            _signals.Incoming.ApplyLanguage.RemoveListener(_view.ShowCurrent);
        }

        private void OnOptionClicked(SettingsOption option)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _internalSignals.ToggleOption.Dispatch(option);
        }

        private void OnLanguagePicked(string code)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.LanguagePicked.Dispatch(code);
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
