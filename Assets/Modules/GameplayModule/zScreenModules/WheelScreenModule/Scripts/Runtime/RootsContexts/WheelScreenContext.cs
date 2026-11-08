using FlowIoC.BaseModule.Controller.Commands;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.WheelScreenModule.Controllers;
using Modules.GameplayModule.WheelScreenModule.Models;
using Modules.GameplayModule.WheelScreenModule.Signals;
using Modules.GameplayModule.WheelScreenModule.ViewsMediators;

namespace Modules.GameplayModule.WheelScreenModule.RootsContexts
{
    public class WheelScreenContext : ScreenSubContext<WheelScreenView, WheelScreenMediator>
    {
        // Layer 2: a popup over the main menu.
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 2,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("WheelScreen"),
            HasShowAnimation = false,
            HasHideAnimation = true,
        };

        private WheelScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<WheelScreenSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<WheelModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.ApplyEvents).ToSequence<StoreWheelEventsCommand>();
            CommandBinder.Bind(_signals.Incoming.OpenWheelScreen)
                .ToSequence<OpenWheelScreenCommand>()
                .ToSequence<SignalDispatchCommand>(_signals.Outgoing.EventsRequested);
        }
    }
}
