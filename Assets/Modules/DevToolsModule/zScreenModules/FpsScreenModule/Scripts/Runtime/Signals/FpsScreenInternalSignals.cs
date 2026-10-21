using FlowIoC.BaseModule.Signals;

namespace Modules.DevToolsModule.FpsScreenModule.Signals
{
    /// <summary>What the module says to its own commands.</summary>
    internal class FpsScreenInternalSignals : ISignalHolder
    {
        /// <summary>Show or hide the readout as the developer switch stands now.</summary>
        public Signal Refresh = new();
    }
}
