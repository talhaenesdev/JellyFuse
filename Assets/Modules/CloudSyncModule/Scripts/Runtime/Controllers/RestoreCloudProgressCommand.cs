using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.AccountModule.Services;
using Modules.CloudSaveModule.Services;
using Modules.CloudSyncModule.Constants;
using Modules.CloudSyncModule.Signals;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.LoadingModule.Services;
using Modules.ProfileModule.Shared.Data.UnityObjects;
using UnityEngine;

namespace Modules.CloudSyncModule.Controllers
{
    /// <summary>
    /// The boot's Cloud step. Waits a moment for the sign-in, reads the cloud copy, and takes it over
    /// this device's when it is further ahead - a higher level, so a player never loses progress to
    /// an older copy. A tie keeps this device's: it is the one being played. Offline, signed out or
    /// nothing in the cloud, the step is skipped and the boot goes on with the local save.
    /// </summary>
    internal sealed class RestoreCloudProgressCommand : Command
    {
        [Inject] private IAccountService _account { get; set; }
        [Inject] private ICloudSaveService _cloud { get; set; }
        [Inject] private ILoadingService _loading { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private CloudSyncSignals _signals { get; set; }

        /// <summary>Restored, kept local, skipped, threw - every way out resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            ILoadingStep step = _loading.Report(CloudSyncConstants.CLOUD_STEP);
            step.Start();
            try
            {
                var progress = _shared.GetScriptable<SD_JellyProgress>();
                var profile = _shared.GetScriptable<SD_PlayerProfile>();
                if (progress == null || !await SignedInSoon())
                {
                    step.Skip();
                    Release();
                    return;
                }

                var cloud = await _cloud.LoadAsync(new[] { CloudSyncConstants.PROGRESS, CloudSyncConstants.PROFILE });
                if (cloud == null || !cloud.TryGetValue(CloudSyncConstants.PROGRESS, out string progressJson))
                {
                    step.Skip();
                    Release();
                    return;
                }

                var cloudProgress = ScriptableObject.CreateInstance<SD_JellyProgress>();
                JsonUtility.FromJsonOverwrite(progressJson, cloudProgress);
                bool cloudAhead = cloudProgress.LevelIndex > progress.LevelIndex;
                UnityEngine.Object.Destroy(cloudProgress);

                if (!cloudAhead)
                {
                    step.Complete();
                    Release();
                    return;
                }

                // The step after this one writes both assets to the local save.
                JsonUtility.FromJsonOverwrite(progressJson, progress);
                if (profile != null && cloud.TryGetValue(CloudSyncConstants.PROFILE, out string profileJson))
                    JsonUtility.FromJsonOverwrite(profileJson, profile);
                FlowLogger.Log($"Restored the cloud copy: level {progress.LevelIndex + 1}, {progress.Coins} coins.");

                var signals = _signals;
                step.Complete();
                Release();
                signals.Outgoing.Restored.Dispatch();
            }
            catch (Exception exception)
            {
                // The cloud is a backup, not the game: a fault here must not hold the boot on a
                // retry screen, so the step is skipped and the local save is played.
                FlowLogger.LogError($"RestoreCloudProgressCommand threw while restoring: {exception}");
                step.Skip();
                Stop();
            }
        }

        // The sign-in runs beside the boot already; this only waits on the one in flight, and gives
        // up after a few seconds so an offline launch is not held back.
        private async Task<bool> SignedInSoon()
        {
            Task<bool> signIn = _account.SignInAsync();
            Task wait = Task.Delay(TimeSpan.FromSeconds(CloudSyncConstants.SIGN_IN_WAIT_SECONDS));
            return await Task.WhenAny(signIn, wait) == signIn && signIn.Result;
        }
    }
}
