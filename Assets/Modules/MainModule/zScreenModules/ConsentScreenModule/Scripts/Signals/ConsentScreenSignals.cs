using FlowIoC.BaseModule.Signals;

namespace Modules.MainModule.ConsentScreenModule.Signals
{
    public class ConsentScreenSignals : ISignalHolder
    {
        public ConsentScreenSignalsIncoming Incoming = new();
        public ConsentScreenSignalsOutgoing Outgoing = new();
    }

    public class ConsentScreenSignalsIncoming
    {
        /// <summary>Ask the player whether ads and analytics may use personal data.</summary>
        public Signal OpenConsentScreen = new();
    }

    public class ConsentScreenSignalsOutgoing
    {
        /// <summary>The panel has closed itself with the player's answer: true agreed, false refused.</summary>
        public Signal<bool> Answered = new();
    }
}
