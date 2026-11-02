using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.LoginRewardsScreenModule.Signals
{
    public class LoginRewardsScreenSignals : ISignalHolder
    {
        public LoginRewardsScreenSignalsIncoming Incoming = new();
        public LoginRewardsScreenSignalsOutgoing Outgoing = new();
    }

    public class LoginRewardsScreenSignalsIncoming
    {
        public Signal OpenLoginRewardsScreen = new();
        /// <summary>How the daily events stand now; the screen keeps the last and redraws while open.</summary>
        public Signal<EventsRVO> ApplyEvents = new();
    }

    public class LoginRewardsScreenSignalsOutgoing
    {
        /// <summary>The screen opened and wants the events brought up to date.</summary>
        public Signal EventsRequested = new();
        /// <summary>The claim button was pressed: today's reward is asked for.</summary>
        public Signal ClaimClicked = new();
        public Signal Closed = new();
    }
}
