using Modules.MainModule.EventsScreenModule.Signals;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.MainModule.EventsScreenModule.ViewsMediators;
using Modules.MainModule.EventsScreenModule.Controllers;

namespace Modules.MainModule.EventsScreenModule.RootsContexts
{
    public class EventsScreenContext : ScreenSubContext<EventsScreenView, EventsScreenMediator>
    {
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 0,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("EventsScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

		private EventsScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
			_signals = InjectionBinderCrossContext.Bind<EventsScreenSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.OpenEventsScreen)
                .ToSequence<OpenEventsScreenCommand>();
        }
    }
}
