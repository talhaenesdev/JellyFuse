using Modules.MainModule.ConsentScreenModule.Controllers;
using Modules.MainModule.ConsentScreenModule.Signals;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.MainModule.ConsentScreenModule.ViewsMediators;

namespace Modules.MainModule.ConsentScreenModule.RootsContexts
{
    public class ConsentScreenContext : ScreenSubContext<ConsentScreenView, ConsentScreenMediator>
    {
        // Layer 7: over the menu and any popup the boot opens beside it, under the loading overlay.
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 7,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("ConsentScreen"),
            HasShowAnimation = false,
            HasHideAnimation = true,
        };

        private ConsentScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
        	_signals = InjectionBinderCrossContext.Bind<ConsentScreenSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.OpenConsentScreen).ToSequence<OpenConsentScreenCommand>();
        }
    }
}
