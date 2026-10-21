#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Connectors;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.SettingsScreenModule.Signals;
using Modules.LocalizationModule.Signals;

namespace Modules.GameplayModule.SettingsScreenModule.SettingsScreenTestModule.RootsContexts
{
    /// <summary>
    /// Opens the settings screen the way the game does, through its incoming signal, so the opening
    /// Command fills it. The language it picks goes to the localization service in the scene and
    /// comes back, as SettingsConnectorSubContext wires it in the game.
    /// </summary>
    [ExcludeFromContextWindow]
    public class SettingsScreenTestContext : BaseScreenContext
    {
        private const string GROUP = nameof(SettingsScreenTestContext);
        private SettingsScreenSignals _settings;

        public override void Setup()
        {
            base.Setup();
            _settings = InjectionBinderCrossContext.GetInstance<SettingsScreenSignals>();
            var localization = InjectionBinderCrossContext.GetInstance<LocalizationSignals>();
            _settings.Outgoing.LanguagePicked.Connect(localization.Incoming.SetLanguage, GROUP);
            localization.Outgoing.LanguageChanged.Connect(_settings.Incoming.ApplyLanguage, GROUP);
        }

        public override void Launch()
        {
            base.Launch();
            _settings.Incoming.OpenSettingsScreen.Dispatch();
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
#endif
