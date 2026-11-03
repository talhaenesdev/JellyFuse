using FlowIoC.BaseModule.Controller.Commands;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.ScratchScreenModule.Controllers;
using Modules.GameplayModule.ScratchScreenModule.Models;
using Modules.GameplayModule.ScratchScreenModule.Signals;
using Modules.GameplayModule.ScratchScreenModule.ViewsMediators;

namespace Modules.GameplayModule.ScratchScreenModule.RootsContexts
{
    public class ScratchScreenContext : ScreenSubContext<ScratchScreenView, ScratchScreenMediator>
    {
        // Layer 2: a popup over the main menu.
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 2,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("ScratchScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private ScratchScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<ScratchScreenSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<ScratchModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.ApplyEvents).ToSequence<StoreScratchEventsCommand>();
            CommandBinder.Bind(_signals.Incoming.OpenScratchScreen)
                .ToSequence<OpenScratchScreenCommand>()
                .ToSequence<SignalDispatchCommand>(_signals.Outgoing.EventsRequested);
        }
    }
}
