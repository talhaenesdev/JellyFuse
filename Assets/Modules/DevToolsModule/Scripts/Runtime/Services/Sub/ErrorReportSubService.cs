using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.ConsoleModule;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using UnityEngine;

namespace Modules.DevToolsModule.Services.Sub
{
    /// <summary>
    /// Sends the errors a released game runs into to the developer's Telegram, through the DevAlert Cloud Code module.
    /// Only a few per run, each message once, its first line cut short: enough to hear that something broke, never a
    /// flood. Nothing is sent from the Editor, and nothing about the player goes with it.
    /// </summary>
    internal sealed class ErrorReportSubService : IConstructable
    {
        private const string ALERT_MODULE = "DevAlert";
        private const string ALERT_ERROR = "ErrorReported";
        private const int MAX_PER_RUN = 3;
        private const int MAX_LENGTH = 300;

        private readonly HashSet<string> _sent = new();
        private bool _sending;

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public void PostConstruct()
        {
#if !UNITY_EDITOR
            Application.logMessageReceived += OnLog;
#endif
        }

        public void Deconstruct()
        {
#if !UNITY_EDITOR
            Application.logMessageReceived -= OnLog;
#endif
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (_sent.Count >= MAX_PER_RUN || string.IsNullOrEmpty(message)) return;
            string line = message.Split('\n')[0].Trim();
            if (line.Length > MAX_LENGTH) line = line.Substring(0, MAX_LENGTH);
            // The same error over and over is reported once; an error raised while reporting is never reported.
            if (_sending || !_sent.Add(line)) return;
            if (type == LogType.Exception && !string.IsNullOrEmpty(stackTrace))
            {
                string where = stackTrace.Split('\n')[0].Trim();
                if (where.Length > 0) line += " @ " + where;
            }
            _ = Send(line);
        }

        private async Task Send(string message)
        {
            _sending = true;
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn) return;
                await CloudCodeService.Instance.CallModuleEndpointAsync<bool>(ALERT_MODULE, ALERT_ERROR, new Dictionary<string, object>
                {
                    { "message", message },
                    { "version", Application.version },
                    { "device", SystemInfo.deviceModel },
                });
            }
            catch (Exception exception)
            {
                // Offline, or the module not deployed: the game goes on, the developer just hears nothing.
                FlowLogger.Log($"ErrorReportSubService - the error could not be reported: {exception.Message}");
            }
            finally
            {
                _sending = false;
            }
        }
    }
}
