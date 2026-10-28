using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.MainModule.MainScreenModule.Signals;

namespace Modules.MainModule.MainScreenModule.ViewsMediators
{
    public class MainScreenMediator : IMediator
    {
        [Inject] private MainScreenView _view { get; set; }
        [InjectSignal] private MainScreenSignals _signals { get; set; }

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
            _view.PlayClicked += OnPlayClicked;
            _view.SettingsClicked += OnSettingsClicked;
            _view.HomeClicked += OnHomeClicked;
            _view.NavTabClicked += OnNavTabClicked;
            _view.ProfileIconClicked += OnProfileIconClicked;
            _view.LivesPlusClicked += OnLivesPlusClicked;
            _signals.Incoming.ApplyNextLevel.AddListener(_view.ApplyNextLevel);
            _signals.Incoming.ApplyLanguage.AddListener(_view.ApplyLanguage);
            _signals.Incoming.ApplyCoins.AddListener(_view.ApplyCoins);
            _signals.Incoming.ApplyLives.AddListener(_view.ApplyLives);
            _signals.Incoming.ApplyProfileIcon.AddListener(_view.ApplyProfileIcon);
            _signals.Incoming.ApplyProfileFrame.AddListener(_view.ApplyProfileFrame);
            _signals.Outgoing.ProfileIconRequested.Dispatch();
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.PlayClicked -= OnPlayClicked;
            _view.SettingsClicked -= OnSettingsClicked;
            _view.HomeClicked -= OnHomeClicked;
            _view.NavTabClicked -= OnNavTabClicked;
            _view.ProfileIconClicked -= OnProfileIconClicked;
            _view.LivesPlusClicked -= OnLivesPlusClicked;
            _signals.Incoming.ApplyNextLevel.RemoveListener(_view.ApplyNextLevel);
            _signals.Incoming.ApplyLanguage.RemoveListener(_view.ApplyLanguage);
            _signals.Incoming.ApplyCoins.RemoveListener(_view.ApplyCoins);
            _signals.Incoming.ApplyLives.RemoveListener(_view.ApplyLives);
            _signals.Incoming.ApplyProfileIcon.RemoveListener(_view.ApplyProfileIcon);
            _signals.Incoming.ApplyProfileFrame.RemoveListener(_view.ApplyProfileFrame);
        }

        private void OnPlayClicked()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _view.Hide();
            _signals.Outgoing.PlayClicked.Dispatch();
        }

        private void OnSettingsClicked()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.SettingsClicked.Dispatch();
        }

        private void OnNavTabClicked(int index)
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            if (index == 2)
            {
                _view.Hide();
                _signals.Outgoing.EventsClicked.Dispatch();
                return;
            }
            _signals.Outgoing.NavTabClicked.Dispatch(index);
        }

        private void OnHomeClicked()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.HomeClicked.Dispatch();
        }

        private void OnLivesPlusClicked()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.LivesPlusClicked.Dispatch();
        }
        private void OnProfileIconClicked()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal))
                _signals.Outgoing.ProfileIconClicked.Dispatch();
        }
    }
}
