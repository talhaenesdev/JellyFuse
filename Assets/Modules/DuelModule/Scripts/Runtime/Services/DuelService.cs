using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using Modules.DuelModule.Data.ValueObjects;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;

namespace Modules.DuelModule.Services
{
    public sealed class DuelService : IDuelService
    {
        private const string MODULE = "Leaderboard";
        private const string FIND = "FindDuel";
        private const string SUBMIT = "SubmitDuel";
        /// <summary>A cold server answers late; the search screen waits this long before it gives up.</summary>
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(12);

        private sealed class SubmitResult
        {
            public bool Accepted { get; set; }
            public string Reason { get; set; }
        }

        public async Task<DuelMatchVO> FindAsync(int maxLevel)
        {
            if (!SignedIn()) return null;
            try
            {
                var match = await WithTimeout(CloudCodeService.Instance.CallModuleEndpointAsync<DuelMatchVO>(MODULE, FIND,
                    new Dictionary<string, object> { { "maxLevel", maxLevel } }));
                if (match == null) FlowLogger.Log("DuelService - no opponent came back in time.");
                return match != null && match.Found ? match : null;
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"DuelService - no opponent, the server could not be reached: {exception.Message}");
                return null;
            }
        }

        public async Task<bool> SubmitAsync(int level, IReadOnlyList<float> exits)
        {
            if (!SignedIn()) return false;
            try
            {
                var result = await WithTimeout(CloudCodeService.Instance.CallModuleEndpointAsync<SubmitResult>(MODULE, SUBMIT,
                    new Dictionary<string, object> { { "level", level }, { "exits", exits } }));
                if (result == null || !result.Accepted) FlowLogger.Log($"DuelService - the run on level {level} was not filed: {result?.Reason}");
                return result != null && result.Accepted;
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"DuelService - the run on level {level} was not filed: {exception.Message}");
                return false;
            }
        }

        public bool IsReady => UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;

        public async Task<bool> SendChallengeAsync(string targetId, int level, IReadOnlyList<float> exits)
        {
            if (!SignedIn()) return false;
            try
            {
                var result = await WithTimeout(CloudCodeService.Instance.CallModuleEndpointAsync<SubmitResult>(MODULE, "SendChallenge",
                    new Dictionary<string, object> { { "targetId", targetId }, { "level", level }, { "exits", exits } }));
                if (result == null || !result.Accepted) FlowLogger.Log($"DuelService - the challenge was not sent: {result?.Reason}");
                return result != null && result.Accepted;
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"DuelService - the challenge was not sent: {exception.Message}");
                return false;
            }
        }

        public async Task<ChallengeMailVO> GetChallengesAsync()
        {
            if (!IsReady) return null;
            try
            {
                return await WithTimeout(CloudCodeService.Instance.CallModuleEndpointAsync<ChallengeMailVO>(MODULE, "GetChallenges",
                    new Dictionary<string, object>()));
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"DuelService - challenges could not be read: {exception.Message}");
                return null;
            }
        }

        public async Task<bool> AnswerChallengeAsync(string challengeId, bool declined, float seconds)
        {
            if (!SignedIn()) return false;
            try
            {
                var result = await WithTimeout(CloudCodeService.Instance.CallModuleEndpointAsync<SubmitResult>(MODULE, "AnswerChallenge",
                    new Dictionary<string, object> { { "challengeId", challengeId }, { "declined", declined }, { "seconds", seconds } }));
                return result != null && result.Accepted;
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"DuelService - the challenge answer did not reach the server: {exception.Message}");
                return false;
            }
        }

        private static async Task<T> WithTimeout<T>(Task<T> call) where T : class
        {
            var finished = await Task.WhenAny(call, Task.Delay(TIMEOUT));
            if (finished != call)
            {
                _ = call.ContinueWith(late => _ = late.Exception, TaskContinuationOptions.OnlyOnFaulted);
                return null;
            }
            return await call;
        }

        private static bool SignedIn()
        {
            bool signedIn = UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;
            if (!signedIn) FlowLogger.Log($"DuelService - not signed in (services {UnityServices.State}).");
            return signedIn;
        }
    }
}
