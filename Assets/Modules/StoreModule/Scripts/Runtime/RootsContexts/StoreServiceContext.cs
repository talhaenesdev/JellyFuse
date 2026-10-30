using FlowIoC.BaseModule.Contexts;
using Modules.StoreModule.Controllers;
using Modules.StoreModule.Models;
using Modules.StoreModule.Services;
using Modules.StoreModule.Signals;

namespace Modules.StoreModule.RootsContexts
{
    public class StoreServiceContext : Context
    {
        private StoreSignals _signals;
        private StoreInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<StoreSignals>();
            _internalSignals = InjectionBinder.Bind<StoreInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<StoreModel>();
            InjectionBinderCrossContext.Bind<IStoreService, StoreService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_internalSignals.Connect).ToSequence<ConnectStoreCommand>();
            CommandBinder.Bind(_signals.Incoming.Buy).ToSequence<BuyProductCommand>();
            CommandBinder.Bind(_signals.Incoming.RequestPrices).ToSequence<AnnouncePricesCommand>();
        }

        public override void Launch()
        {
            base.Launch();
            _internalSignals.Connect.Dispatch();
        }
    }
}
