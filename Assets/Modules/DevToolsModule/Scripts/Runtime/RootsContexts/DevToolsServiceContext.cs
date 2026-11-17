using FlowIoC.BaseModule.Contexts;
using Modules.DevToolsModule.Models;
using Modules.DevToolsModule.Services;
using Modules.DevToolsModule.Services.Sub;

namespace Modules.DevToolsModule.RootsContexts
{
    public class DevToolsServiceContext : Context
    {
        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<DevToolsModel>();
            InjectionBinder.Bind<DevErrorLogModel>();
            InjectionBinder.Bind<ErrorReportSubService>();
            InjectionBinderCrossContext.Bind<IDevToolsService, DevToolsService>();
        }

        public override void Setup()
        {
            base.Setup();
            // Made now rather than when the panel first asks, so it hears the errors logged before then.
            InjectionBinder.GetInstance<DevErrorLogModel>();
            // Likewise the reporter, so a released game's first errors reach the developer.
            InjectionBinder.GetInstance<ErrorReportSubService>();
        }
    }
}
