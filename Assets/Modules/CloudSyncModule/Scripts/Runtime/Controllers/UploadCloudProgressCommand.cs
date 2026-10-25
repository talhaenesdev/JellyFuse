using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.AccountModule.Services;
using Modules.CloudSaveModule.Services;
using Modules.CloudSyncModule.Constants;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.ProfileModule.Shared.Data.UnityObjects;
using UnityEngine;

namespace Modules.CloudSyncModule.Controllers
{
    /// <summary>
    /// Sends the progress and the profile to the cloud as they are now. Signed out or offline it does
    /// nothing: the local save already holds the same values, and the next upload sends them.
    /// </summary>
    internal sealed class UploadCloudProgressCommand : Command
    {
        [Inject] private IAccountService _account { get; set; }
        [Inject] private ICloudSaveService _cloud { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        /// <summary>Uploaded, nothing to send, offline, threw - every way out resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            try
            {
                var progress = _shared.GetScriptable<SD_JellyProgress>();
                if (!_account.IsSignedIn || progress == null)
                {
                    Release();
                    return;
                }

                var records = new Dictionary<string, string> { [CloudSyncConstants.PROGRESS] = JsonUtility.ToJson(progress) };
                var profile = _shared.GetScriptable<SD_PlayerProfile>();
                if (profile != null) records[CloudSyncConstants.PROFILE] = JsonUtility.ToJson(profile);

                await _cloud.SaveAsync(records);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"UploadCloudProgressCommand threw while uploading: {exception}");
                Stop();
            }
        }
    }
}
