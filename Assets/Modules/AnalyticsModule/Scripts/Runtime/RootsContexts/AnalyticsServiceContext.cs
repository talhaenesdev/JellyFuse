using FlowIoC.BaseModule.Contexts;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.RootsContexts
{
    /// <summary>
    /// Collection does not start by itself: it waits for the player's consent answer, and the module that asks
    /// binds <see cref="IAnalyticsService.Commands.Start"/> to that answer.
    /// </summary>
    public class AnalyticsServiceContext : Context
    {
        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinderCrossContext.Bind<IAnalyticsService, AnalyticsService>();
        }
    }
}
