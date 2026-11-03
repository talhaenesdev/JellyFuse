using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.ScratchScreenModule.Signals
{
    public class ScratchScreenSignals : ISignalHolder
    {
        public ScratchScreenSignalsIncoming Incoming = new();
        public ScratchScreenSignalsOutgoing Outgoing = new();
    }

    public class ScratchScreenSignalsIncoming
    {
        public Signal OpenScratchScreen = new();
        /// <summary>How the daily events stand now; the screen keeps the last and redraws while open.</summary>
        public Signal<EventsRVO> ApplyEvents = new();
        /// <summary>A card was dealt and paid: what lies under its cells. The open card is laid for scratching.</summary>
        public Signal<ScratchCardRVO> CardDealt = new();
    }

    public class ScratchScreenSignalsOutgoing
    {
        /// <summary>The screen opened and wants the events brought up to date.</summary>
        public Signal EventsRequested = new();
        /// <summary>A card was asked for: the free one, else one an ad gave.</summary>
        public Signal CardClicked = new();
        /// <summary>An ad was asked for, to get one more card.</summary>
        public Signal AdCardClicked = new();
        public Signal Closed = new();
    }
}
