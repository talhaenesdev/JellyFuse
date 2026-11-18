using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.DuelScreenModule.Signals;
using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.DuelScreenModule.ViewsMediators
{
    public class DuelScreenMediator : IMediator
    {
        [Inject] private DuelScreenView _view { get; set; }
        [InjectSignal] private DuelScreenSignals _signals { get; set; }

        public void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;
        }

        public void OnRemove()
        {
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;
        }

        private void OnScreenShown(IScreenBody screen)
        {
            _view.CloseClicked += OnCloseClicked;
            _signals.Incoming.ShowUnavailable.AddListener(_view.ShowUnavailable);
            _signals.Incoming.ApplyProgress.AddListener(_view.ApplyProgress);
            _signals.Incoming.ShowResult.AddListener(OnResult);
            _signals.Incoming.Close.AddListener(OnClose);
            _view.ChallengeAccepted += OnAccepted;
            _view.ChallengeDeclined += OnDeclined;
            _view.Idle += OnIdle;
        }

        private void OnAccepted(string id)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.ChallengeAccepted.Dispatch(id);
        }

        private void OnDeclined(string id)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.ChallengeDeclined.Dispatch(id);
        }

        // Nothing of the duel left on show: the screen goes back to its pool.
        private void OnIdle()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _view.Hide();
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.ChallengeAccepted -= OnAccepted;
            _view.ChallengeDeclined -= OnDeclined;
            _view.Idle -= OnIdle;
            _view.CloseClicked -= OnCloseClicked;
            _signals.Incoming.ShowUnavailable.RemoveListener(_view.ShowUnavailable);
            _signals.Incoming.ApplyProgress.RemoveListener(_view.ApplyProgress);
            _signals.Incoming.ShowResult.RemoveListener(OnResult);
            _signals.Incoming.Close.RemoveListener(OnClose);
        }

        private void OnResult(DuelResultRVO result) => _view.ShowResult(result);

        private void OnCloseClicked()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _view.Hide();
            _signals.Outgoing.SearchClosed.Dispatch();
        }

        private void OnClose()
        {
            if (!_view.Data.HasState(ScreenState.AvailableToSendSignal)) return;
            _view.Hide();
        }
    }
}
