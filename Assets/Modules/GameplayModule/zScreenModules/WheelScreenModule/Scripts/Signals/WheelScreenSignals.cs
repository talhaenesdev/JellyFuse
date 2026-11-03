using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.WheelScreenModule.Signals
{
    public class WheelScreenSignals : ISignalHolder
    {
        public WheelScreenSignalsIncoming Incoming = new();
        public WheelScreenSignalsOutgoing Outgoing = new();
    }

    public class WheelScreenSignalsIncoming
    {
        public Signal OpenWheelScreen = new();
        /// <summary>How the daily events stand now - the wheel's slices and the spins left; the screen keeps the last and redraws while open.</summary>
        public Signal<EventsRVO> ApplyEvents = new();
        /// <summary>A spin was given: the slice it landed on, clockwise from the top. The open wheel turns to it.</summary>
        public Signal<int> WheelSpun = new();
    }

    public class WheelScreenSignalsOutgoing
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
