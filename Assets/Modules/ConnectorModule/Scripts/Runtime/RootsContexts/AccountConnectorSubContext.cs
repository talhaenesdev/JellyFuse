using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.AccountModule.Signals;
using Modules.MainModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// Signs the player in beside the boot: BootStarted is the boot's fan-out for what may run without
    /// slowing it, so the loading bar never waits on the network and an offline launch plays at once.
    /// </summary>
    public class AccountConnectorSubContext : Context
    {
        private const string GROUP = nameof(AccountConnectorSubContext);
        private AccountSignals _accountSignals;
        private MainSignals _mainSignals;

        public override void Setup()
        {
            base.Setup();
            _accountSignals = InjectionBinderCrossContext.GetInstance<AccountSignals>();
            _mainSignals = InjectionBinderCrossContext.GetInstance<MainSignals>();
            IncomingSignals();
        }

        private void IncomingSignals()
        {
            _mainSignals.Outgoing.BootStarted.Connect(_accountSignals.Incoming.SignIn, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
