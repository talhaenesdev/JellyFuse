using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.DailyQuestsScreenModule.Signals
{
    public class DailyQuestsScreenSignals : ISignalHolder
    {
        public DailyQuestsScreenSignalsIncoming Incoming = new();
        public DailyQuestsScreenSignalsOutgoing Outgoing = new();
    }

    public class DailyQuestsScreenSignalsIncoming
    {
        public Signal OpenDailyQuestsScreen = new();
        /// <summary>How the daily events stand now; the screen keeps the last and redraws while open.</summary>
        public Signal<EventsRVO> ApplyEvents = new();
    }

    public class DailyQuestsScreenSignalsOutgoing
    {
        /// <summary>The screen opened and wants the events brought up to date.</summary>
        public Signal EventsRequested = new();
        /// <summary>A finished quest's claim button was pressed: its position in today's list.</summary>
        public Signal<int> ClaimClicked = new();
        public Signal Closed = new();
    }
}
