#if UNITY_EDITOR
using FlowIoC.BaseModule.Contexts;
using Modules.LocalizationModule.LocalizationTestModule.Controllers;
using Modules.LocalizationModule.LocalizationTestModule.Signals;

namespace Modules.LocalizationModule.LocalizationTestModule.RootsContexts
{
    /// <summary>Runs the module's checks once the scene is up, against the test module's own sample texts.</summary>
    public class LocalizationTestContext : Context
    {
        private LocalizationTestInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinder.Bind<LocalizationTestInternalSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Verify).ToSequence<VerifyLocalizationCommand>();
        }

        public override void Launch()
        {
            base.Launch();
            _signals.Verify.Dispatch();
        }
    }
}
#endif
