using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using Modules.DuelModule.Data.ValueObjects;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.Subscriptions;
using Unity.Services.Core;

namespace Modules.DuelModule.Services
{
    public sealed class DuelService : IDuelService
    {
        private const string MODULE = "Leaderboard";
        private const string FIND = "FindDuel";
        private const string SUBMIT = "SubmitDuel";
        /// <summary>A cold server answers late - its first call after a quiet spell can take most of this - so it is waited on this long.</summary>
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(20);

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

        public async Task<bool> SubmitAsync(int level, IReadOnlyList<float> exits, string fingerprint)
        {
            if (!SignedIn()) return false;
            try
            {
                var result = await WithTimeout(CloudCodeService.Instance.CallModuleEndpointAsync<SubmitResult>(MODULE, SUBMIT,
                    new Dictionary<string, object> { { "level", level }, { "exits", exits }, { "fp", fingerprint ?? string.Empty } }));
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

        private sealed class LiveReply
        {
            public bool Ok { get; set; }
            public string Reason { get; set; }
            public string MatchId { get; set; }
            public bool Won { get; set; }
            public string TargetId { get; set; }
            public string Name { get; set; }
            public string Icon { get; set; }
            public int Level { get; set; }
        }

        private bool _listening;
        private ISubscriptionEvents _subscription;

        public bool IsListening => _listening;

        public async Task<bool> ListenAsync(Action<string, string> onMessage)
        {
            if (_listening) return true;
            if (!IsReady || onMessage == null) return false;
            try
            {
                // A dropped connection is let go of before a new one is made, so messages never arrive twice.
                if (_subscription != null)
                {
                    try { await _subscription.UnsubscribeAsync(); }
                    catch (Exception) { /* already gone */ }
                    _subscription = null;
                }
                var callbacks = new SubscriptionEventCallbacks();
                // Messages arrive on the main thread already (Wire dispatches on Unity's), so they go straight on.
                callbacks.MessageReceived += message => onMessage(message.MessageType, message.Message);
                callbacks.Error += error => FlowLogger.Log($"DuelService - live messages: {error}");
                callbacks.Kicked += () => _listening = false;
                // Lost for good, not just reconnecting: the next listen makes a new connection.
                callbacks.ConnectionStateChanged += state =>
                {
                    if (state == EventConnectionState.Unsubscribed || state == EventConnectionState.Error) _listening = false;
                };
                _subscription = await CloudCodeService.Instance.SubscribeToPlayerMessagesAsync(callbacks);
                _listening = true;
                return true;
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"DuelService - could not listen for live duels: {exception.Message}");
                return false;
            }
        }

        public async Task<LiveOpponentVO> FindLiveAsync(int minLevel)
        {
            var reply = await Live("FindLive", new Dictionary<string, object> { { "minLevel", minLevel } });
            if (reply == null || !reply.Ok || string.IsNullOrEmpty(reply.TargetId)) return null;
            return new LiveOpponentVO { PlayerId = reply.TargetId, Name = reply.Name ?? string.Empty, Icon = reply.Icon ?? string.Empty, Level = reply.Level };
        }

        public async Task<string> InviteLiveAsync(string targetId, int level)
        {
            var reply = await Live("InviteLive", new Dictionary<string, object> { { "targetId", targetId }, { "level", level } });
            return reply != null && reply.Ok ? reply.MatchId : null;
        }

        public async Task<bool> AnswerLiveAsync(string matchId, bool accept)
        {
            var reply = await Live("AnswerLive", new Dictionary<string, object> { { "matchId", matchId }, { "accept", accept } });
            return reply != null && reply.Ok;
        }

        public Task CancelLiveAsync(string matchId) =>
            Live("CancelLive", new Dictionary<string, object> { { "matchId", matchId } });

        public Task ProgressLiveAsync(string matchId, int done, int total) =>
            Live("ProgressLive", new Dictionary<string, object> { { "matchId", matchId }, { "done", done }, { "total", total } });

        public Task FinishLiveAsync(string matchId, float seconds) =>
            Live("FinishLive", new Dictionary<string, object> { { "matchId", matchId }, { "seconds", seconds } });

        private async Task<LiveReply> Live(string function, Dictionary<string, object> arguments)
        {
            if (!IsReady) return null;
            try
            {
                var reply = await WithTimeout(CloudCodeService.Instance.CallModuleEndpointAsync<LiveReply>(MODULE, function, arguments));
                if (reply != null && !reply.Ok) FlowLogger.Log($"DuelService - {function}: {reply.Reason}");
                return reply;
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"DuelService - {function} did not reach the server: {exception.Message}");
                return null;
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
