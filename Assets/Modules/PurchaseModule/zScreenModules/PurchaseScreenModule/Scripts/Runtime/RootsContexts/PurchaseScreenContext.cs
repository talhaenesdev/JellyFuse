using Modules.PurchaseModule.PurchaseScreenModule.Controllers;
using Modules.PurchaseModule.PurchaseScreenModule.Signals;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.PurchaseModule.PurchaseScreenModule.ViewsMediators;

namespace Modules.PurchaseModule.PurchaseScreenModule.RootsContexts
{
    public class PurchaseScreenContext : ScreenSubContext<PurchaseScreenView, PurchaseScreenMediator>
    {
        // Layer 2: over the main menu and the gameplay screen, which stay open underneath.
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 2,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("PurchaseScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

		private PurchaseScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
			_signals = InjectionBinderCrossContext.Bind<PurchaseScreenSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.Open).ToSequence<OpenPurchaseScreenCommand>();
        }
    }
}
