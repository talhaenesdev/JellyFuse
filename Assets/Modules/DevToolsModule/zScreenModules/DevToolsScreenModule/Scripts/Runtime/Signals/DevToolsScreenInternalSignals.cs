using FlowIoC.BaseModule.Signals;

namespace Modules.DevToolsModule.DevToolsScreenModule.Signals
{
    /// <summary>
    /// What the module says to its own commands. None of these leave the module, so they sit apart
    /// from the public holder in Scripts/Signals rather than widening what the module offers.
    /// </summary>
    internal class DevToolsScreenInternalSignals : ISignalHolder
    {
        /// <summary>The developer asked for a switch to change: its key and the value asked for.</summary>
        public Signal<string, bool> SetToggle = new();
        /// <summary>A switch now holds this value.</summary>
        public Signal<string, bool> ToggleChanged = new();
    }
}
