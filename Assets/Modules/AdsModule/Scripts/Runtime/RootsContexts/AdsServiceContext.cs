using FlowIoC.BaseModule.Contexts;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;

namespace Modules.AdsModule.RootsContexts
{
    /// <summary>
    /// LevelPlay does not start by itself: it waits for the player's consent answer, and the module that asks
    /// binds <see cref="IAdsService.Commands.Start"/> to that answer.
    /// </summary>
    public class AdsServiceContext : Context
    {
        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<AdsModel>();
            InjectionBinderCrossContext.Bind<IAdsService, AdsService>();
        }
    }
}
