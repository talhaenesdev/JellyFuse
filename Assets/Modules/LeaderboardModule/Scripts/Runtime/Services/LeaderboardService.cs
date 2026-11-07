using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using Modules.LeaderboardModule.Data.ValueObjects;
using Modules.LeaderboardModule.Enums;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;

namespace Modules.LeaderboardModule.Services
{
    public sealed class LeaderboardService : ILeaderboardService
    {
        private const string MODULE = "Leaderboard";
        private const string SUBMIT = "SubmitLevel";
        private const string BOARD = "GetBoard";
        private const string PROFILE = "SetProfile";
        /// <summary>How long a call may take before the caller is told nothing came back; the server is never waited on for ever.</summary>
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(8);
        /// <summary>
        /// A board reads every listed player's profile as well, and the module's first call after a quiet spell
        /// starts it up cold, so it gets longer - and a second try, the first having woken the server.
        /// </summary>
        private static readonly TimeSpan BOARD_TIMEOUT = TimeSpan.FromSeconds(12);

        private sealed class SubmitResult
        {
            public bool Accepted { get; set; }
            public string Reason { get; set; }
            public int Points { get; set; }
            public int Best { get; set; }
            public long Total { get; set; }
            public bool Record { get; set; }
        }

        private sealed class ProfileResult
        {
            public bool Saved { get; set; }
            public string Name { get; set; }
        }

        public async Task<LevelScoreVO> SubmitLevelAsync(int level, int difficulty, int secondsTotal, int secondsLeft, bool extraTime)
        {
            if (!SignedIn()) return null;
            try
            {
                var result = await WithTimeout(CloudCodeService.Instance.CallModuleEndpointAsync<SubmitResult>(MODULE, SUBMIT, new Dictionary<string, object>
                {
                    { "level", level },
                    { "difficulty", difficulty },
                    { "secondsTotal", secondsTotal },
                    { "secondsLeft", secondsLeft },
                    { "extraTime", extraTime },
                }));
                if (result == null || !result.Accepted)
                {
                    FlowLogger.Log($"LeaderboardService - level {level} not scored: {result?.Reason}");
                    return null;
                }
                return new LevelScoreVO { Points = result.Points, Best = result.Best, Total = result.Total, Record = result.Record };
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"LeaderboardService - level {level} not scored, the server could not be reached: {exception.Message}");
                return null;
            }
        }

        public async Task<LeaderboardPageVO> GetBoardAsync(LeaderboardBoard board, int limit)
        {
            if (!SignedIn()) return null;
            try
            {
                var arguments = new Dictionary<string, object>
                {
                    { "board", board == LeaderboardBoard.Weekly ? "weekly" : "all_time" },
                    { "limit", limit },
                };
                var page = await WithTimeout(CloudCodeService.Instance.CallModuleEndpointAsync<LeaderboardPageVO>(MODULE, BOARD, arguments), BOARD_TIMEOUT);
                if (page == null)
                {
                    FlowLogger.Log("LeaderboardService - the board was slow; asking once more.");
                    page = await WithTimeout(CloudCodeService.Instance.CallModuleEndpointAsync<LeaderboardPageVO>(MODULE, BOARD, arguments), BOARD_TIMEOUT);
                }
                if (page == null) FlowLogger.Log("LeaderboardService - the board did not come back in time.");
                return page;
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"LeaderboardService - the board could not be read: {exception.Message}");
                return null;
            }
        }

        public async Task<bool> SetProfileAsync(string name, string iconId, string frameId)
        {
            if (!SignedIn()) return false;
            try
            {
                var call = CloudCodeService.Instance.CallModuleEndpointAsync<ProfileResult>(MODULE, PROFILE, new Dictionary<string, object>
                {
                    { "name", name ?? string.Empty },
                    { "icon", iconId ?? string.Empty },
                    { "frame", frameId ?? string.Empty },
                });
                bool sent = (await WithTimeout(call))?.Saved == true;
                if (!sent) FlowLogger.Log("LeaderboardService - the profile did not reach the server in time.");
                return sent;
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"LeaderboardService - the profile could not be sent: {exception.Message}");
                return false;
            }
        }

        /// <summary>The call's answer, or null when it takes longer than TIMEOUT; a late answer is dropped.</summary>
        private static async Task<T> WithTimeout<T>(Task<T> call, TimeSpan? timeout = null) where T : class
        {
            var finished = await Task.WhenAny(call, Task.Delay(timeout ?? TIMEOUT));
            if (finished != call)
            {
                _ = call.ContinueWith(late => _ = late.Exception, TaskContinuationOptions.OnlyOnFaulted);
                return null;
            }
            return await call;
        }

        private static bool SignedIn() =>
            UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;
    }
}
