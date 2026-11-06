using FlowIoC.BaseModule.Contexts;
using Modules.LeaderboardModule.Services;

namespace Modules.LeaderboardModule.RootsContexts
{

    public class LeaderboardServiceContext : Context
    {
        public override void SignalBindings()
        {
            base.SignalBindings();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinderCrossContext.Bind<ILeaderboardService, LeaderboardService>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
        }

        public override void Setup()
        {
            base.Setup();
        }

        public override void Launch()
        {
            base.Launch();
        }
    }
}
