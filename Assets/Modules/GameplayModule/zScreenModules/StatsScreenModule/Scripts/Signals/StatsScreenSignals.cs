using FlowIoC.BaseModule.Signals;

namespace Modules.GameplayModule.StatsScreenModule.Signals
{
    public class StatsScreenSignals : ISignalHolder
    {
        public StatsScreenSignalsIncoming Incoming = new();
        public StatsScreenSignalsOutgoing Outgoing = new();
    }

    public class StatsScreenSignalsIncoming
    {
        /// <summary>Open the stats popup on the player's totals.</summary>
        public Signal OpenStatsScreen = new();
    }

    public class StatsScreenSignalsOutgoing
    {
        public Signal Closed = new();
    }
}
