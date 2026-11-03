using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.SlotScreenModule.Signals
{
    public class SlotScreenSignals : ISignalHolder
    {
        public SlotScreenSignalsIncoming Incoming = new();
        public SlotScreenSignalsOutgoing Outgoing = new();
    }

    public class SlotScreenSignalsIncoming
    {
        public Signal OpenSlotScreen = new();
        /// <summary>How the daily events stand now; the screen keeps the last and redraws while open.</summary>
        public Signal<EventsRVO> ApplyEvents = new();
        /// <summary>A spin was given: where each reel stops and what it paid. The open machine spins to it.</summary>
        public Signal<SlotResultRVO> SlotSpun = new();
    }

    public class SlotScreenSignalsOutgoing
    {
        /// <summary>The screen opened and wants the events brought up to date.</summary>
        public Signal EventsRequested = new();
        /// <summary>A spin was asked for: the free one, else one an ad gave.</summary>
        public Signal SpinClicked = new();
        /// <summary>An ad was asked for, to get one more spin.</summary>
        public Signal AdSpinClicked = new();
        public Signal Closed = new();
    }
}
