using System;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LeaderboardModule.Services;
using Modules.MainModule.LeaderboardScreenModule.Models;
using Modules.MainModule.LeaderboardScreenModule.Signals;

namespace Modules.MainModule.LeaderboardScreenModule.Controllers
{
    /// <summary>
    /// Fetches the board the screen shows and announces it. The last copy that came back shows at once, so the
    /// screen does not sit on Loading while the server wakes; the fresh one replaces it when it comes. Offline with
    /// no copy, it announces that nothing came back.
    /// </summary>
    // One call waited on for up to 12 seconds twice, while the server is slow to wake.
    [LongRetain(30)]
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
            var model = _model;
            var signals = _signals;
            try
            {
                var cached = model.Cached(board);
                if (cached != null) signals.BoardLoaded.Dispatch(board, cached);
                else signals.BoardLoading.Dispatch(board);
                var page = await _leaderboard.GetBoardAsync(board, ROWS);
                Release();
                model.Remember(board, page);
                // A copy already on screen stays when the fresh one did not come.
                if (page != null || cached == null) signals.BoardLoaded.Dispatch(board, page);
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"LoadLeaderboardBoardCommand threw while reading the board: {exception}");
                Stop();
            }
        }
    }
}
