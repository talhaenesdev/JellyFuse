using Modules.GameplayModule.TargetHuntScreenModule.Signals;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.TargetHuntScreenModule.ViewsMediators;

namespace Modules.GameplayModule.TargetHuntScreenModule.RootsContexts
{
    public class TargetHuntScreenContext : ScreenSubContext<TargetHuntScreenView, TargetHuntScreenMediator>
    {
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 3,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("TargetHuntScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

		private TargetHuntScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
			_signals = InjectionBinderCrossContext.Bind<TargetHuntScreenSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
        }
    }
}
