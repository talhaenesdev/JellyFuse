using FlowIoC.BaseModule.Signals;

namespace Modules.MainModule.EventsScreenModule.Signals
{
    public class EventsScreenSignals : ISignalHolder
    {
        public EventsScreenSignalsIncoming Incoming = new();
        public EventsScreenSignalsOutgoing Outgoing = new();
    }

    public class EventsScreenSignalsIncoming
    {
        public Signal OpenEventsScreen = new();
    }

    public class EventsScreenSignalsOutgoing
    {
        public Signal BackClicked = new();
        /// <summary>The duel card's play button.</summary>
        public Signal DuelClicked = new();
    }
}
