using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.LeaderboardModule.Enums;
using Modules.MainModule.LeaderboardScreenModule.Signals;

namespace Modules.MainModule.LeaderboardScreenModule.ViewsMediators
{
    public class LeaderboardScreenMediator : IMediator
    {
        [Inject] private LeaderboardScreenView _view { get; set; }
        [InjectSignal] private LeaderboardScreenSignals _signals { get; set; }
        [InjectSignal] private LeaderboardScreenInternalSignals _internalSignals { get; set; }

        public void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;
            // A board can arrive before the show completes, so these listen for as long as the screen is registered.
            _internalSignals.BoardLoading.AddListener(_view.ShowLoading);
            _internalSignals.BoardLoaded.AddListener(_view.ShowBoard);
        }

        public void OnRemove()
        {
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;
            _internalSignals.BoardLoading.RemoveListener(_view.ShowLoading);
            _internalSignals.BoardLoaded.RemoveListener(_view.ShowBoard);
        }

        private void OnScreenShown(IScreenBody screen)
        {
            _view.BackClicked += OnBackClicked;
            _view.TabClicked += OnTabClicked;
            _view.EditProfileClicked += OnEditProfileClicked;
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.BackClicked -= OnBackClicked;
            _view.TabClicked -= OnTabClicked;
            _view.EditProfileClicked -= OnEditProfileClicked;
        }

        private void OnEditProfileClicked()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.EditProfileClicked.Dispatch();
        }

        private void OnBackClicked()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _view.Hide();
            _signals.Outgoing.BackClicked.Dispatch();
        }

        private void OnTabClicked(LeaderboardBoard board)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _internalSignals.TabClicked.Dispatch(board);
        }
    }
}
