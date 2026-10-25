using FlowIoC.BaseModule.Contexts;
using Modules.AccountModule.Controllers;
using Modules.AccountModule.Services;
using Modules.AccountModule.Signals;

namespace Modules.AccountModule.RootsContexts
{
    public class AccountServiceContext : Context
    {
        private AccountSignals _signals;

        private AccountInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _internalSignals = InjectionBinder.Bind<AccountInternalSignals>();
            _signals = InjectionBinderCrossContext.Bind<AccountSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinderCrossContext.Bind<IAccountService, AccountService>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.SignIn).ToSequence<SignInCommand>();
        }

        public override void Setup()
        {
            base.Setup();
        }

        public override void Launch()
        {
            base.Launch();
        }
    }
}
