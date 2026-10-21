using FlowIoC.BaseModule.Contexts;
using Modules.LocalizationModule.Controllers;
using Modules.LocalizationModule.Models;
using Modules.LocalizationModule.Services;
using Modules.LocalizationModule.Signals;

namespace Modules.LocalizationModule.RootsContexts
{
    public class LocalizationServiceContext : Context
    {
        private LocalizationSignals _signals;
        private LocalizationInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _internalSignals = InjectionBinder.Bind<LocalizationInternalSignals>();
            _signals = InjectionBinderCrossContext.Bind<LocalizationSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<ILocalizationModel, LocalizationModel>();
            // The one type other modules reference directly, which is what makes this a Service.
            InjectionBinderCrossContext.Bind<ILocalizationService, LocalizationService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_internalSignals.Initialize).ToSequence<InitializeLocalizationCommand>();
            // A change asked for by the game's code (the service) or by another module (Incoming) is the same step.
            CommandBinder.Bind(_internalSignals.SetLanguage).ToSequence<ChangeLanguageCommand>();
            CommandBinder.Bind(_signals.Incoming.SetLanguage).ToSequence<ChangeLanguageCommand>();
        }

        public override void Setup()
        {
            base.Setup();
            // The module brings itself up: the language picked before any screen launches.
            _internalSignals.Initialize.Dispatch();
        }
    }
}
