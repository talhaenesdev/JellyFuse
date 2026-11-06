using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LeaderboardModule.Services;
using Modules.MainModule.LeaderboardScreenModule.Models;
using Modules.MainModule.LeaderboardScreenModule.Signals;

namespace Modules.MainModule.LeaderboardScreenModule.Controllers
{
    /// <summary>Fetches the board the screen shows and announces it; offline, it announces that nothing came back.</summary>
    internal sealed class LoadLeaderboardBoardCommand : Command
    {
        private const int ROWS = 50;

        [Inject] private ILeaderboardService _leaderboard { get; set; }
        [Inject] private LeaderboardScreenModel _model { get; set; }
        [InjectSignal] private LeaderboardScreenInternalSignals _signals { get; set; }

        public override async void Execute()
        {
            Retain();
            // Read into locals: the step ends before the board is announced.
            var board = _model.Board;
            var signals = _signals;
            try
            {
                signals.BoardLoading.Dispatch(board);
                var page = await _leaderboard.GetBoardAsync(board, ROWS);
                Release();
                signals.BoardLoaded.Dispatch(board, page);
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"LoadLeaderboardBoardCommand threw while reading the board: {exception}");
                Stop();
            }
        }
    }
}
