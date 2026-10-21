using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.GameplayScreenModule.Signals;
using Modules.LocalizationModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>Tells the screens that show text when the game's language changes, so they redraw it.</summary>
    public class LocalizationConnectorSubContext : Context
    {
        private const string GROUP = nameof(LocalizationConnectorSubContext);
        private LocalizationSignals _localizationSignals;
        private GameplayScreenSignals _gameplayScreenSignals;
        public override void Setup()
        {
            base.Setup();
            _localizationSignals = InjectionBinderCrossContext.GetInstance<LocalizationSignals>();
            _gameplayScreenSignals = InjectionBinderCrossContext.GetInstance<GameplayScreenSignals>();
            OutgoingSignals();
        }
        private void OutgoingSignals()
        {
            _localizationSignals.Outgoing.LanguageChanged.Connect(_gameplayScreenSignals.Incoming.ApplyLanguage, GROUP);
        }
        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
