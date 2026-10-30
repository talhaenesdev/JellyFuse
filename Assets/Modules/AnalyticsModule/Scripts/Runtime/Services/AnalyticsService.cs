using System;
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using Unity.Services.Analytics;
using Unity.Services.Core;

namespace Modules.AnalyticsModule.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        public async void StartCollecting()
        {
#if !UNITY_EDITOR
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
                Unity.Services.Analytics.AnalyticsService.Instance.StartDataCollection();
            }
            catch (Exception exception)
            {
                FlowLogger.LogWarning($"Analytics did not start: {exception.Message}");
            }
#else
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

        public void Record(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
#if UNITY_EDITOR
            FlowLogger.Log($"Event {eventName} {(parameters == null ? "" : string.Join(", ", parameters))}");
#else
            var custom = new CustomEvent(eventName);
            if (parameters != null)
                foreach (var pair in parameters) custom[pair.Key] = pair.Value;
            Unity.Services.Analytics.AnalyticsService.Instance.RecordEvent(custom);
#endif
        }
    }
}
