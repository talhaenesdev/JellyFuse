using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LeaderboardModule.Enums;
using Modules.LeaderboardModule.Services;
using Modules.MainModule.LeaderboardScreenModule.Models;

namespace Modules.MainModule.LeaderboardScreenModule.Controllers
{
    /// <summary>
    /// At boot, once the player is signed in, the all-time board is fetched in the background and kept: the call
    /// wakes the server, and the first time the screen opens it already has a board to show.
    /// </summary>
    // Up to a minute for the sign-in, then up to 24 seconds for a cold server.
    [LongRetain(90)]
    internal sealed class PrefetchLeaderboardCommand : Command
    {
        private const int ROWS = 50;
        private const int SIGN_IN_WAIT_SECONDS = 60;

        [Inject] private ILeaderboardService _leaderboard { get; set; }
        [Inject] private LeaderboardScreenModel _model { get; set; }

        public override async void Execute()
        {
            Retain();
            var leaderboard = _leaderboard;
            var model = _model;
            try
            {
                for (int waited = 0; !leaderboard.IsReady && waited < SIGN_IN_WAIT_SECONDS; waited++) await Task.Delay(1000);
                if (!leaderboard.IsReady)
                {
                    FlowLogger.Log("PrefetchLeaderboardCommand - never signed in; the board is fetched when the screen opens.");
                    Release();
                    return;
                }
                var page = await leaderboard.GetBoardAsync(LeaderboardBoard.AllTime, ROWS);
                Release();
                model.Remember(LeaderboardBoard.AllTime, page);
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"PrefetchLeaderboardCommand threw while fetching the board: {exception}");
                Stop();
            }
        }
    }
}
