using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UgsCloudSave = Unity.Services.CloudSave.CloudSaveService;

namespace Modules.CloudSaveModule.Services
{
    internal sealed class CloudSaveService : ICloudSaveService
    {
        private static bool SignedIn =>
            UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;

        public async Task<bool> SaveAsync(IReadOnlyDictionary<string, string> records)
        {
            if (!SignedIn || records == null || records.Count == 0) return false;
            var data = new Dictionary<string, object>();
            foreach (var record in records) data[record.Key] = record.Value;
            try
            {
                await UgsCloudSave.Instance.Data.Player.SaveAsync(data);
                return true;
            }
            catch (Exception exception) when (exception is CloudSaveException || exception is RequestFailedException)
            {
                FlowLogger.LogWarning($"CloudSaveService - saving {records.Count} record(s) failed: {exception.Message}");
                return false;
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> LoadAsync(IReadOnlyCollection<string> keys)
        {
            if (!SignedIn || keys == null) return null;
            try
            {
                var items = await UgsCloudSave.Instance.Data.Player.LoadAsync(new HashSet<string>(keys));
                var answer = new Dictionary<string, string>();
                foreach (var item in items) answer[item.Key] = item.Value.Value.GetAsString();
                return answer;
            }
            catch (Exception exception) when (exception is CloudSaveException || exception is RequestFailedException)
            {
                FlowLogger.LogWarning($"CloudSaveService - loading failed: {exception.Message}");
                return null;
            }
        }
    }
}
