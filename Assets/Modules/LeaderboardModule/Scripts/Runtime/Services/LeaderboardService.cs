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

        private sealed class SubmitResult
        {
            public bool Accepted { get; set; }
            public string Reason { get; set; }
            public int Points { get; set; }
            public int Best { get; set; }
            public long Total { get; set; }
            public bool Record { get; set; }
        }

        public async Task<LevelScoreVO> SubmitLevelAsync(int level, int difficulty, int secondsTotal, int secondsLeft, bool extraTime)
        {
            if (!SignedIn()) return null;
            try
            {
                var result = await CloudCodeService.Instance.CallModuleEndpointAsync<SubmitResult>(MODULE, SUBMIT, new Dictionary<string, object>
                {
                    { "level", level },
                    { "difficulty", difficulty },
                    { "secondsTotal", secondsTotal },
                    { "secondsLeft", secondsLeft },
                    { "extraTime", extraTime },
                });
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
                return await CloudCodeService.Instance.CallModuleEndpointAsync<LeaderboardPageVO>(MODULE, BOARD, new Dictionary<string, object>
                {
                    { "board", board == LeaderboardBoard.Weekly ? "weekly" : "all_time" },
                    { "limit", limit },
                });
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"LeaderboardService - the board could not be read: {exception.Message}");
                return null;
            }
        }

        private static bool SignedIn() =>
            UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;
    }
}
