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
        /// <summary>Fetch the board on show again - the player's name or icon changed.</summary>
        public Signal ReloadBoard = new();
    }

    public class LeaderboardScreenSignalsOutgoing
    {
        public Signal BackClicked = new();
        /// <summary>The player tapped their own row to change their name or icon.</summary>
        public Signal EditProfileClicked = new();
    }
}
