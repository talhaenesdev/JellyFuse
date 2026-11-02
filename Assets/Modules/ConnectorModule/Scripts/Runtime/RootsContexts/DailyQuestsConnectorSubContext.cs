using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.DailyQuestsScreenModule.Signals;
using Modules.GameplayModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The daily quests popup: the main menu's quests button opens it, Gameplay's daily events fill it, and its
    /// claim buttons ask Gameplay for a quest's reward.
    /// </summary>
    public class DailyQuestsConnectorSubContext : Context
    {
        private const string GROUP = nameof(DailyQuestsConnectorSubContext);
        private DailyQuestsScreenSignals _questsSignals;
        private GameplaySignals _gameplaySignals;
        private MainScreenSignals _mainScreenSignals;

        public override void Setup()
        {
            base.Setup();
            _questsSignals = InjectionBinderCrossContext.GetInstance<DailyQuestsScreenSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            IncomingSignals();
            OutgoingSignals();
        }

        private void IncomingSignals()
        {
            _mainScreenSignals.Outgoing.QuestsClicked.Connect(_questsSignals.Incoming.OpenDailyQuestsScreen, GROUP);
            _gameplaySignals.Outgoing.EventsChanged.Connect(_questsSignals.Incoming.ApplyEvents, GROUP);
        }

        private void OutgoingSignals()
        {
            _questsSignals.Outgoing.EventsRequested.Connect(_gameplaySignals.Incoming.RequestEvents, GROUP);
            _questsSignals.Outgoing.ClaimClicked.Connect(_gameplaySignals.Incoming.ClaimQuest, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
