using FlowIoC.BaseModule.Signals;

namespace Modules.CloudSyncModule.Signals
{
    public class CloudSyncSignals : ISignalHolder
    {
        public CloudSyncSignalsIncoming Incoming = new();
        public CloudSyncSignalsOutgoing Outgoing = new();
    }

    public class CloudSyncSignalsIncoming
    {
        /// <summary>The boot's restore: take the cloud copy where it is ahead, then upload this device's.</summary>
        public Signal Restore = new();

        /// <summary>Upload the saved assets as they are now.</summary>
        public Signal Upload = new();
    }

    public class CloudSyncSignalsOutgoing
    {
        /// <summary>The cloud copy replaced this device's progress and profile.</summary>
        public Signal Restored = new();
    }
}
