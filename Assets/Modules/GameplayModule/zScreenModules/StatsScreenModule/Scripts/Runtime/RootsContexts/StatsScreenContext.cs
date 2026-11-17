using Modules.GameplayModule.StatsScreenModule.Signals;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.StatsScreenModule.Controllers;
using Modules.GameplayModule.StatsScreenModule.ViewsMediators;

namespace Modules.GameplayModule.StatsScreenModule.RootsContexts
{
    // Layer 2: a popup over the main menu.
    public class StatsScreenContext : ScreenSubContext<StatsScreenView, StatsScreenMediator>
    {
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 2,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("StatsScreen"),
            HasShowAnimation = false,
            HasHideAnimation = true,
        };

		private StatsScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
			_signals = InjectionBinderCrossContext.Bind<StatsScreenSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.OpenStatsScreen).ToSequence<OpenStatsScreenCommand>();
        }
    }
}
