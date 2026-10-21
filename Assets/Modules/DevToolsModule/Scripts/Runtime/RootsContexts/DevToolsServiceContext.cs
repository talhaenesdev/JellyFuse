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
            InjectionBinderCrossContext.Bind<IDevToolsService, DevToolsService>();
        }
    }
}
