using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.Signals;
using Modules.GameplayModule.ScratchScreenModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The scratch card popup: the main menu's scratch button opens it, Gameplay's daily events fill it and Gameplay's
    /// dealt card lays under its foil, and its buttons ask Gameplay for a card, or for an ad that gives one.
    /// </summary>
    public class ScratchConnectorSubContext : Context
    {
        private const string GROUP = nameof(ScratchConnectorSubContext);
        private ScratchScreenSignals _scratchSignals;
        private GameplaySignals _gameplaySignals;
        private MainScreenSignals _mainScreenSignals;

        public override void Setup()
        {
            base.Setup();
            _scratchSignals = InjectionBinderCrossContext.GetInstance<ScratchScreenSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            IncomingSignals();
            OutgoingSignals();
        }

        private void IncomingSignals()
        {
            _mainScreenSignals.Outgoing.ScratchClicked.Connect(_scratchSignals.Incoming.OpenScratchScreen, GROUP);
            _gameplaySignals.Outgoing.EventsChanged.Connect(_scratchSignals.Incoming.ApplyEvents, GROUP);
            _gameplaySignals.Outgoing.ScratchCardDealt.Connect(_scratchSignals.Incoming.CardDealt, GROUP);
        }

        private void OutgoingSignals()
        {
            _scratchSignals.Outgoing.EventsRequested.Connect(_gameplaySignals.Incoming.RequestEvents, GROUP);
            _scratchSignals.Outgoing.CardClicked.Connect(_gameplaySignals.Incoming.DealScratchCard, GROUP);
            _scratchSignals.Outgoing.AdCardClicked.Connect(_gameplaySignals.Incoming.WatchAdForScratchCard, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
