using Modules.MainModule.LeaderboardScreenModule.Signals;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.MainModule.LeaderboardScreenModule.Controllers;
using Modules.MainModule.LeaderboardScreenModule.Models;
using Modules.MainModule.LeaderboardScreenModule.ViewsMediators;

namespace Modules.MainModule.LeaderboardScreenModule.RootsContexts
{
    public class LeaderboardScreenContext : ScreenSubContext<LeaderboardScreenView, LeaderboardScreenMediator>
    {
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 0,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("LeaderboardScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private LeaderboardScreenSignals _signals;
        private LeaderboardScreenInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<LeaderboardScreenSignals>();
            _internalSignals = InjectionBinder.Bind<LeaderboardScreenInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<LeaderboardScreenModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.OpenLeaderboardScreen)
                .ToSequence<OpenLeaderboardScreenCommand>()
                .ToSequence<LoadLeaderboardBoardCommand>();
            CommandBinder.Bind(_internalSignals.TabClicked)
                .ToSequence<SelectLeaderboardBoardCommand>()
                .ToSequence<LoadLeaderboardBoardCommand>();
        }
    }
}
