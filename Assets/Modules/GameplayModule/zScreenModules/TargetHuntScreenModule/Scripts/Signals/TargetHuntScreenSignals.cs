using FlowIoC.BaseModule.Signals;

namespace Modules.GameplayModule.TargetHuntScreenModule.Signals
{
    public class TargetHuntScreenSignals : ISignalHolder
    {
        public TargetHuntScreenSignalsIncoming Incoming = new();
        public TargetHuntScreenSignalsOutgoing Outgoing = new();
    }

    public class TargetHuntScreenSignalsIncoming
    {
    }

    public class TargetHuntScreenSignalsOutgoing
    {
    }
}
