using FlowIoC.BaseModule.Signals;
using Modules.LeaderboardModule.Data.ValueObjects;
using Modules.LeaderboardModule.Enums;

namespace Modules.MainModule.LeaderboardScreenModule.Signals
{
    /// <summary>The screen's own traffic: a tab picked, and a board on its way or arrived.</summary>
    internal class LeaderboardScreenInternalSignals : ISignalHolder
    {
        public Signal<LeaderboardBoard> TabClicked = new();
        public Signal<LeaderboardBoard> BoardLoading = new();
        /// <summary>The board asked for, and what came back: null when the server could not be reached.</summary>
        public Signal<LeaderboardBoard, LeaderboardPageVO> BoardLoaded = new();
    }
}
