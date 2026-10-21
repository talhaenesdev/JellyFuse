using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.SettingsScreenModule.Signals;
using Modules.LocalizationModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The settings screen: the main menu's and the HUD's settings buttons open it, the language it picks goes to the
    /// localization service, and the language in use comes back to it.
    /// </summary>
    public class SettingsConnectorSubContext : Context
    {
        private const string GROUP = nameof(SettingsConnectorSubContext);
        private SettingsScreenSignals _settingsSignals;
        private LocalizationSignals _localizationSignals;
        private MainScreenSignals _mainScreenSignals;
        public override void Setup()
        {
            base.Setup();
            _settingsSignals = InjectionBinderCrossContext.GetInstance<SettingsScreenSignals>();
            _localizationSignals = InjectionBinderCrossContext.GetInstance<LocalizationSignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
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
        }
        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
