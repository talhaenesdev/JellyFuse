using FlowIoC.BaseModule.Contexts;
using Modules.AppUpdateModule.Controllers;
using Modules.AppUpdateModule.Services;
using Modules.AppUpdateModule.Signals;

namespace Modules.AppUpdateModule.RootsContexts
{
    public class AppUpdateServiceContext : Context
    {
        private AppUpdateInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinder.Bind<AppUpdateInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinderCrossContext.Bind<IAppUpdateService, AppUpdateService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.CheckForUpdate).ToSequence<RunMandatoryUpdateCommand>();
        }

        public override void Launch()
        {
            base.Launch();
            // Asked as the game opens; anywhere but a Play Store install answers that there is nothing new.
            _signals.CheckForUpdate.Dispatch();
        }
    }
}
