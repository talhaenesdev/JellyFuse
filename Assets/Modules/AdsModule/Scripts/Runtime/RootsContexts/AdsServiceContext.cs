using FlowIoC.BaseModule.Contexts;
using Modules.AdsModule.Controllers;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;
using Modules.AdsModule.Signals;

namespace Modules.AdsModule.RootsContexts
{
    public class AdsServiceContext : Context
    {
        private AdsInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _internalSignals = InjectionBinder.Bind<AdsInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<AdsModel>();
            InjectionBinderCrossContext.Bind<IAdsService, AdsService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_internalSignals.Initialize).ToSequence<InitializeAdsCommand>();
        }

        public override void Launch()
        {
            base.Launch();
            _internalSignals.Initialize.Dispatch();
        }
    }
}
