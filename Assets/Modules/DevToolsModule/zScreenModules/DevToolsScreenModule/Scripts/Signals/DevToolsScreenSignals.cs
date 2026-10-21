using System.Collections.Generic;
using FlowIoC.BaseModule.Signals;
using Modules.DevToolsModule.Data.ValueObjects;

namespace Modules.DevToolsModule.DevToolsScreenModule.Signals
{
    public class DevToolsScreenSignals : ISignalHolder
    {
        public DevToolsScreenSignalsIncoming Incoming = new();
        public DevToolsScreenSignalsOutgoing Outgoing = new();
    }

    public class DevToolsScreenSignalsIncoming
    {
        /// <summary>The values a tuning page shows as sliders: the page's name and its sliders.</summary>
        public Signal<string, IReadOnlyList<DevSliderVO>> ShowSliders = new();
        public Signal OpenDevToolsScreen = new();
    }

    public class DevToolsScreenSignalsOutgoing
    {
        /// <summary>The panel closed itself.</summary>
        public Signal Closed = new();
        /// <summary>Previous (-1) or Next (+1) was pressed: go to that level.</summary>
        public Signal<int> SkipLevelRequested = new();
        /// <summary>Reset Boosters was pressed: the player's boosters go back to what a new player gets.</summary>
        public Signal ResetBoostersRequested = new();
        /// <summary>Refill Lives was pressed: the player's lives go back to full.</summary>
        public Signal RefillLivesRequested = new();
        /// <summary>Reset Player was pressed: every saved thing goes back to a new player's, and the game closes.</summary>
        public Signal ResetPlayerRequested = new();
        /// <summary>A tuning page opened, by name: whoever owns its values should send them (Incoming.ShowSliders).</summary>
        public Signal<string> SlidersRequested = new();
        /// <summary>A slider on the tuning page moved: its key and its new value.</summary>
        public Signal<string, float> SliderChanged = new();
        /// <summary>Reset was pressed on a tuning page, by name: its values go back to their defaults and are sent again.</summary>
        public Signal<string> SlidersResetRequested = new();

        private readonly Dictionary<string, Signal<bool>> _toggled = new();

        /// <summary>The switch with this key now holds the value carried. One signal per key, so a Connector wires each without an if.</summary>
        public Signal<bool> Toggled(string key)
        {
            if (!_toggled.TryGetValue(key, out var signal)) _toggled[key] = signal = new Signal<bool>();
            return signal;
        }
    }
}
