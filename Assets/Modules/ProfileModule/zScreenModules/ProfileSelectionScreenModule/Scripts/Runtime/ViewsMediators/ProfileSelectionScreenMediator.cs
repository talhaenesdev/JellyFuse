using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.ProfileModule.ProfileSelectionScreenModule.Signals;
using Modules.ProfileModule.Shared.Data.ValueObjects;

namespace Modules.ProfileModule.ProfileSelectionScreenModule.ViewsMediators
{
    public sealed class ProfileSelectionScreenMediator : IMediator
    {
        [Inject] private ProfileSelectionScreenView _view { get; set; }
        [InjectSignal] private ProfileSelectionScreenSignals _signals { get; set; }

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
            _view.SaveRequested += OnSaveRequested;
            _signals.Incoming.ProfileSaved.AddListener(_view.ApplySavedProfile);
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.SaveRequested -= OnSaveRequested;
            _signals.Incoming.ProfileSaved.RemoveListener(_view.ApplySavedProfile);
        }

        private void OnSaveRequested(ProfileEditVO edit)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal))
                _signals.Outgoing.SaveRequested.Dispatch(edit);
        }
    }
}
