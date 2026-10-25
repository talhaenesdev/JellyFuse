using FlowIoC.BaseModule.Signals;

namespace Modules.AccountModule.Signals
{
    public class AccountSignals : ISignalHolder
    {
        public AccountSignalsIncoming Incoming = new();
        public AccountSignalsOutgoing Outgoing = new();
    }

    public class AccountSignalsIncoming
    {
        /// <summary>Sign in, into this device's account or a new anonymous one.</summary>
        public Signal SignIn = new();
    }

    public class AccountSignalsOutgoing
    {
        /// <summary>A player is signed in; the payload is their UGS player id.</summary>
        public Signal<string> SignedIn = new();
    }
}
