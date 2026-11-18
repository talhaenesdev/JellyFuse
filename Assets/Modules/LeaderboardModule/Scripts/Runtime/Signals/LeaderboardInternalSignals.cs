using FlowIoC.BaseModule.Signals;

namespace Modules.LeaderboardModule.Signals
{
    /// <summary>What the module says to its own commands.</summary>
    internal class LeaderboardInternalSignals : ISignalHolder
    {
        /// <summary>One turn of the heartbeat: tell the server the game is open, wait, go again.</summary>
        public Signal Heartbeat = new(hideCommandLog: true);
    }
}
