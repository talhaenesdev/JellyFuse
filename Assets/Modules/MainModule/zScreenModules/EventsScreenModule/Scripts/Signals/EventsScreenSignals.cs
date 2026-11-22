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
        /// <summary>Another tab of the menu was picked: the panel closes.</summary>
        public Signal CloseEventsScreen = new();
        /// <summary>How the events stand: the Star Rush card shows its meter from it.</summary>
        public Signal<Modules.GameplayModule.Shared.Data.ValueObjects.EventsRVO> ApplyEvents = new();
        /// <summary>What a carnival chest just gave.</summary>
        public Signal<Modules.GameplayModule.Shared.Data.ValueObjects.CarnivalPrizeRVO> ApplyCarnivalPrize = new();
    }

    public class EventsScreenSignalsOutgoing
    {
        public Signal BackClicked = new();
        /// <summary>The duel card's play button.</summary>
        public Signal DuelClicked = new();
        /// <summary>The panel is up and wants the events as they stand.</summary>
        public Signal EventsRequested = new();
        /// <summary>The Star Rush card's button with a step ready: pay it.</summary>
        public Signal StarRushClaimClicked = new();
        /// <summary>The Color Carnival card's button with a chest ready: open it.</summary>
        public Signal CarnivalOpenClicked = new();
    }
}
