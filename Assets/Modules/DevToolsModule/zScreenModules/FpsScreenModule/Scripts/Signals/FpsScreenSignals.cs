using FlowIoC.BaseModule.Signals;

namespace Modules.DevToolsModule.FpsScreenModule.Signals
{
    public class FpsScreenSignals : ISignalHolder
    {
        public FpsScreenSignalsIncoming Incoming = new();
        public FpsScreenSignalsOutgoing Outgoing = new();
    }

    public class FpsScreenSignalsIncoming
    {
        /// <summary>Show the readout (true) or take it away (false).</summary>
        public Signal<bool> SetVisible = new();
    }

    public class FpsScreenSignalsOutgoing
    {
    }
}
