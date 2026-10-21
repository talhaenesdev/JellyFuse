using Modules.PurchaseModule.Signals;
using FlowIoC.BaseModule.Contexts;

namespace Modules.PurchaseModule.RootsContexts
{

    public class PurchaseSystemContext : Context
    {
		private PurchaseSignals _signals;

		private PurchaseInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
			_internalSignals = InjectionBinder.Bind<PurchaseInternalSignals>();
			_signals = InjectionBinderCrossContext.Bind<PurchaseSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
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
