using FlowIoC.BaseModule.Controller.Commands;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.SlotScreenModule.Controllers;
using Modules.GameplayModule.SlotScreenModule.Models;
using Modules.GameplayModule.SlotScreenModule.Signals;
using Modules.GameplayModule.SlotScreenModule.ViewsMediators;

namespace Modules.GameplayModule.SlotScreenModule.RootsContexts
{
    public class SlotScreenContext : ScreenSubContext<SlotScreenView, SlotScreenMediator>
    {
        // Layer 2: a popup over the main menu.
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 2,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("SlotScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private SlotScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<SlotScreenSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<SlotModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.ApplyEvents).ToSequence<StoreSlotEventsCommand>();
            CommandBinder.Bind(_signals.Incoming.OpenSlotScreen)
                .ToSequence<OpenSlotScreenCommand>()
                .ToSequence<SignalDispatchCommand>(_signals.Outgoing.EventsRequested);
        }
    }
}
