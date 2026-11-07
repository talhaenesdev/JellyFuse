using FlowIoC.BaseModule.Contexts;
using Modules.DevToolsModule.Models;
using Modules.DevToolsModule.Services;

namespace Modules.DevToolsModule.RootsContexts
{
    public class DevToolsServiceContext : Context
    {
        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<DevToolsModel>();
            InjectionBinder.Bind<DevErrorLogModel>();
            InjectionBinderCrossContext.Bind<IDevToolsService, DevToolsService>();
        }

        public override void Setup()
        {
            base.Setup();
            // Made now rather than when the panel first asks, so it hears the errors logged before then.
            InjectionBinder.GetInstance<DevErrorLogModel>();
        }
    }
}
