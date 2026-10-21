using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.GameplayScreenModule.Signals;
using Modules.GameplayModule.PauseScreenModule.Signals;
using Modules.GameplayModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The pause panel: the HUD's settings button opens it, opening it stops the level's clock,
    /// Resume starts it again, and Quit closes the level for the main menu.
    /// </summary>
    public class PauseConnectorSubContext : Context
    {
        private const string GROUP = nameof(PauseConnectorSubContext);
        private PauseScreenSignals _pauseSignals;
        private GameplayScreenSignals _gameplayScreenSignals;
        private GameplaySignals _gameplaySignals;
        public override void Setup()
        {
            base.Setup();
            _pauseSignals = InjectionBinderCrossContext.GetInstance<PauseScreenSignals>();
            _gameplayScreenSignals = InjectionBinderCrossContext.GetInstance<GameplayScreenSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            IncomingSignals();
            OutgoingSignals();
        }
        private void IncomingSignals()
        {
            _gameplayScreenSignals.Outgoing.PauseClicked.Connect(_pauseSignals.Incoming.OpenPauseScreen, GROUP);
        }
        private void OutgoingSignals()
        {
            _pauseSignals.Outgoing.Paused.Connect(_gameplaySignals.Incoming.Stop, GROUP);
            _pauseSignals.Outgoing.Resumed.Connect(_gameplaySignals.Incoming.Resume, GROUP);
            _pauseSignals.Outgoing.QuitClicked.Connect(_gameplayScreenSignals.Incoming.Quit, GROUP);
        }
        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
