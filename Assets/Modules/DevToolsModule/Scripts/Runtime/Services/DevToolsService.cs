using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DevToolsModule.Data.ValueObjects;
using Modules.DevToolsModule.Models;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using UnityEngine;

namespace Modules.DevToolsModule.Services
{
    internal sealed class DevToolsService : IDevToolsService
    {
        private const string ALERT_MODULE = "DevAlert";
        private const string ALERT_PIN = "PinEntered";

        [Inject] private DevToolsModel _model { get; set; }
        [Inject] private DevErrorLogModel _errors { get; set; }

        public IReadOnlyList<DevToggleCVO> Toggles => _model.Toggles;
        public bool PanelEnabled => _model.PanelEnabled;
        public bool Unlocked => _model.Unlocked;
        public float CornerSequenceSeconds => _model.CornerSequenceSeconds;
        public float CornerZone => _model.CornerZone;
        public bool TryUnlock(string pin) => _model.TryUnlock(pin);
        public bool EnabledInBuild => _model.EnabledInBuild;
        public bool TakeOpening() => _model.TakeOpening();
        public bool IsOn(string key) => _model.IsOn(key);
        public bool Set(string key, bool on) => _model.Set(key, on);
        public IReadOnlyList<string> Errors() => _errors.Entries();
        public void ClearErrors() => _errors.Clear();

        public void ReportPinEntry(bool accepted)
        {
#if !UNITY_EDITOR
            _ = SendPinEntry(accepted);
#endif
        }

        /// <summary>The PIN entry with what the device says about itself - nothing that names the player.</summary>
        private static async Task SendPinEntry(bool accepted)
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
                {
                    FlowLogger.Log("DevToolsService - not signed in; the PIN entry was not reported.");
                    return;
                }
                await CloudCodeService.Instance.CallModuleEndpointAsync<bool>(ALERT_MODULE, ALERT_PIN, new Dictionary<string, object>
                {
                    { "accepted", accepted },
                    { "device", SystemInfo.deviceModel },
                    { "version", Application.version },
                    { "os", SystemInfo.operatingSystem },
                    { "language", Application.systemLanguage.ToString() },
                    { "region", Region() },
                    { "memory", SystemInfo.systemMemorySize.ToString() },
                    { "screen", $"{Screen.width}x{Screen.height}" },
                });
            }
            catch (Exception exception)
            {
                // Offline, or the module not deployed: the panel works the same, the developer just hears nothing.
                FlowLogger.Log($"DevToolsService - the PIN entry could not be reported: {exception.Message}");
            }
        }

        /// <summary>The country the device's own settings name; "-" where the runtime cannot tell.</summary>
        private static string Region()
        {
            try { return System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName; }
            catch (Exception) { return "-"; }
        }
    }
}
