using System;

namespace Modules.AppUpdateModule.Services
{
    /// <summary>
    /// Google Play's in-app update. Answers only on an Android build installed from Play; everywhere
    /// else every question comes back as "nothing to update".
    /// </summary>
    public interface IAppUpdateService
    {
        /// <summary>Whether Play has a newer version that may be installed as an immediate update.</summary>
        void FetchImmediateUpdate(Action<bool> onAnswered);

        /// <summary>
        /// Opens Play's full-screen update. On success Play restarts the app, so the callback is only
        /// heard when it did not happen: true when the player backed out, false when it failed.
        /// </summary>
        void StartImmediateUpdate(Action<bool> onNotUpdated);
    }
}
