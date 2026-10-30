using System;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif

namespace Modules.AppUpdateModule.Services
{
    public class AppUpdateService : IAppUpdateService
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private AppUpdateManager _manager;
        private AppUpdateInfo _info;

        public void FetchImmediateUpdate(Action<bool> onAnswered)
        {
            _manager ??= new AppUpdateManager();
            _manager.GetAppUpdateInfo().Completed += operation =>
            {
                _info = operation.IsSuccessful ? operation.GetResult() : null;
                onAnswered(_info != null
                           && _info.UpdateAvailability is UpdateAvailability.UpdateAvailable
                               or UpdateAvailability.DeveloperTriggeredUpdateInProgress
                           && _info.IsUpdateTypeAllowed(AppUpdateOptions.ImmediateAppUpdateOptions()));
            };
        }

        public void StartImmediateUpdate(Action<bool> onNotUpdated)
        {
            if (_info == null)
            {
                onNotUpdated(false);
                return;
            }

            _manager.StartUpdate(_info, AppUpdateOptions.ImmediateAppUpdateOptions()).Completed += request =>
                onNotUpdated(request.Error == AppUpdateErrorCode.ErrorUserCanceled);
        }
#else
        public void FetchImmediateUpdate(Action<bool> onAnswered) => onAnswered(false);

        public void StartImmediateUpdate(Action<bool> onNotUpdated) => onNotUpdated(false);
#endif
    }
}
