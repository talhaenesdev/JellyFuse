using FlowIoC.BaseModule.Signals;

namespace Modules.PurchaseModule.Signals
{
    public class PurchaseSignals : ISignalHolder
    {
        public PurchaseSignalsIncoming Incoming = new();
        public PurchaseSignalsOutgoing Outgoing = new();
    }

    public class PurchaseSignalsIncoming
    {
    }

    public class PurchaseSignalsOutgoing
    {
    }
}
