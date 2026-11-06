using FlowIoC.BaseModule.Signals;

namespace Modules.MainModule.LeaderboardScreenModule.Signals
{
    public class LeaderboardScreenSignals : ISignalHolder
    {
        public LeaderboardScreenSignalsIncoming Incoming = new();
        public LeaderboardScreenSignalsOutgoing Outgoing = new();
    }

    public class LeaderboardScreenSignalsIncoming
    {
        public Signal OpenLeaderboardScreen = new();
    }

    public class LeaderboardScreenSignalsOutgoing
    {
        public Signal BackClicked = new();
    }
}
