using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.SettingsScreenModule.Signals;
using Modules.LocalizationModule.Signals;
using Modules.MainModule.ConsentScreenModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The settings screen: the main menu's and the HUD's settings buttons open it, the language it picks goes to the
    /// localization service, and the language in use comes back to it. Its privacy row opens the consent question.
    /// </summary>
    public class SettingsConnectorSubContext : Context
    {
        private const string GROUP = nameof(SettingsConnectorSubContext);
        private SettingsScreenSignals _settingsSignals;
        private LocalizationSignals _localizationSignals;
        private MainScreenSignals _mainScreenSignals;
        private ConsentScreenSignals _consentSignals;
        public override void Setup()
        {
            base.Setup();
            _settingsSignals = InjectionBinderCrossContext.GetInstance<SettingsScreenSignals>();
            _localizationSignals = InjectionBinderCrossContext.GetInstance<LocalizationSignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            _consentSignals = InjectionBinderCrossContext.GetInstance<ConsentScreenSignals>();
            IncomingSignals();
            OutgoingSignals();
        }
        private void IncomingSignals()
        {
            _mainScreenSignals.Outgoing.SettingsClicked.Connect(_settingsSignals.Incoming.OpenSettingsScreen, GROUP);
            _localizationSignals.Outgoing.LanguageChanged.Connect(_settingsSignals.Incoming.ApplyLanguage, GROUP);
        }
        private void OutgoingSignals()
        {
            _settingsSignals.Outgoing.LanguagePicked.Connect(_localizationSignals.Incoming.SetLanguage, GROUP);
            // The privacy row asks the consent question again; its answer goes to Main as the first one did.
            _settingsSignals.Outgoing.PrivacyClicked.Connect(_consentSignals.Incoming.OpenConsentScreen, GROUP);
        }
        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
