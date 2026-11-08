using FlowIoC.BaseModule.Controller.Commands;
using Modules.GameplayModule.DailyQuestsScreenModule.Signals;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.DailyQuestsScreenModule.Controllers;
using Modules.GameplayModule.DailyQuestsScreenModule.Models;
using Modules.GameplayModule.DailyQuestsScreenModule.ViewsMediators;

namespace Modules.GameplayModule.DailyQuestsScreenModule.RootsContexts
{
    public class DailyQuestsScreenContext : ScreenSubContext<DailyQuestsScreenView, DailyQuestsScreenMediator>
    {
        // Layer 2: a popup over the main menu.
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 2,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("DailyQuestsScreen"),
            HasShowAnimation = false,
            HasHideAnimation = true,
        };

		private DailyQuestsScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
			_signals = InjectionBinderCrossContext.Bind<DailyQuestsScreenSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<DailyQuestsModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.ApplyEvents).ToSequence<StoreQuestsCommand>();
            CommandBinder.Bind(_signals.Incoming.OpenDailyQuestsScreen)
                .ToSequence<OpenDailyQuestsScreenCommand>()
                .ToSequence<SignalDispatchCommand>(_signals.Outgoing.EventsRequested);
        }
    }
}
