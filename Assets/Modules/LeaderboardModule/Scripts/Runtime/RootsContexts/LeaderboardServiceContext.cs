using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller.Commands;
using Modules.LeaderboardModule.Controllers;
using Modules.LeaderboardModule.Signals;
using Modules.LeaderboardModule.Services;

namespace Modules.LeaderboardModule.RootsContexts
{

    public class LeaderboardServiceContext : Context
    {
        private LeaderboardInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _internalSignals = InjectionBinder.Bind<LeaderboardInternalSignals>();
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
            // Paced by itself: each beat waits for the one before, then dispatches the next.
            CommandBinder.Bind(_internalSignals.Heartbeat)
                .ToSequence<SendHeartbeatCommand>()
                .ToSequence<SignalDispatchCommand>(_internalSignals.Heartbeat);
        }

        public override void Setup()
        {
            base.Setup();
        }

        public override void Launch()
        {
            base.Launch();
            _internalSignals.Heartbeat.Dispatch();
        }
    }
}
