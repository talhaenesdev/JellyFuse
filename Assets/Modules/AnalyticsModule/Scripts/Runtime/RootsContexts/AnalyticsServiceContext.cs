using FlowIoC.BaseModule.Contexts;
using Modules.AnalyticsModule.Controllers;
using Modules.AnalyticsModule.Services;
using Modules.AnalyticsModule.Signals;

namespace Modules.AnalyticsModule.RootsContexts
{
    public class AnalyticsServiceContext : Context
    {
        private AnalyticsInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinder.Bind<AnalyticsInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinderCrossContext.Bind<IAnalyticsService, AnalyticsService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.StartCollecting).ToSequence<StartAnalyticsCommand>();
        }

        public override void Launch()
        {
            base.Launch();
            _signals.StartCollecting.Dispatch();
        }
    }
}
