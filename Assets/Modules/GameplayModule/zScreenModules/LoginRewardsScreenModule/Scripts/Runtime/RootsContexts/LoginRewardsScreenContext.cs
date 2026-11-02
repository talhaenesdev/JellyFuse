using FlowIoC.BaseModule.Controller.Commands;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.LoginRewardsScreenModule.Controllers;
using Modules.GameplayModule.LoginRewardsScreenModule.Models;
using Modules.GameplayModule.LoginRewardsScreenModule.Signals;
using Modules.GameplayModule.LoginRewardsScreenModule.ViewsMediators;

namespace Modules.GameplayModule.LoginRewardsScreenModule.RootsContexts
{
    public class LoginRewardsScreenContext : ScreenSubContext<LoginRewardsScreenView, LoginRewardsScreenMediator>
    {
        // Layer 2: a popup over the main menu.
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 2,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("LoginRewardsScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private LoginRewardsScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<LoginRewardsScreenSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<LoginRewardsModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.ApplyEvents).ToSequence<StoreLoginRewardsCommand>();
            CommandBinder.Bind(_signals.Incoming.OpenLoginRewardsScreen)
                .ToSequence<OpenLoginRewardsScreenCommand>()
                .ToSequence<SignalDispatchCommand>(_signals.Outgoing.EventsRequested);
        }
    }
}
