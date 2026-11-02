using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.LoginRewardsScreenModule.Signals;
using Modules.GameplayModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The daily rewards popup: the main menu's rewards button opens it, Gameplay's daily events fill it, and its
    /// claim button asks Gameplay for today's reward. The main menu hears the same events for its dots and badge.
    /// </summary>
    public class LoginRewardsConnectorSubContext : Context
    {
        private const string GROUP = nameof(LoginRewardsConnectorSubContext);
        private LoginRewardsScreenSignals _loginSignals;
        private GameplaySignals _gameplaySignals;
        private MainScreenSignals _mainScreenSignals;

        public override void Setup()
        {
            base.Setup();
            _loginSignals = InjectionBinderCrossContext.GetInstance<LoginRewardsScreenSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            IncomingSignals();
            OutgoingSignals();
        }

        private void IncomingSignals()
        {
            _mainScreenSignals.Outgoing.LoginRewardsClicked.Connect(_loginSignals.Incoming.OpenLoginRewardsScreen, GROUP);
            _gameplaySignals.Outgoing.EventsChanged.Connect(_loginSignals.Incoming.ApplyEvents, GROUP);
            _gameplaySignals.Outgoing.EventsChanged.Connect(_mainScreenSignals.Incoming.ApplyEvents, GROUP);
        }

        private void OutgoingSignals()
        {
            _loginSignals.Outgoing.EventsRequested.Connect(_gameplaySignals.Incoming.RequestEvents, GROUP);
            _loginSignals.Outgoing.ClaimClicked.Connect(_gameplaySignals.Incoming.ClaimLoginReward, GROUP);
            _mainScreenSignals.Outgoing.EventsRequested.Connect(_gameplaySignals.Incoming.RequestEvents, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
