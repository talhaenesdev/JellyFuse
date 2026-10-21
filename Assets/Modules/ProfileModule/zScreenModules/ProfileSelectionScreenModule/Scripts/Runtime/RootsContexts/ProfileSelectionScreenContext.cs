using Modules.ProfileModule.ProfileSelectionScreenModule.Signals;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.ProfileModule.ProfileSelectionScreenModule.ViewsMediators;
using Modules.ProfileModule.ProfileSelectionScreenModule.Controllers;

namespace Modules.ProfileModule.ProfileSelectionScreenModule.RootsContexts
{
    public class ProfileSelectionScreenContext : ScreenSubContext<ProfileSelectionScreenView, ProfileSelectionScreenMediator>
    {
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 1,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("ProfileSelectionScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

		private ProfileSelectionScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
			_signals = InjectionBinderCrossContext.Bind<ProfileSelectionScreenSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.Open).ToSequence<OpenProfileSelectionScreenCommand>();
        }
    }
}
