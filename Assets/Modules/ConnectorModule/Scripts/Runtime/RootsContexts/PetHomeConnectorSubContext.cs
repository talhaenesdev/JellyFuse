using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;
using Modules.PetHomeModule.PetHomeScreenModule.Signals;
using Modules.PetHomeModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The pet home: the main menu's button opens it, its screen shows it and asks for what the player did, and its
    /// purchases are paid from Gameplay's purse, whose coins the screen shows.
    /// </summary>
    public class PetHomeConnectorSubContext : Context
    {
        private const string GROUP = nameof(PetHomeConnectorSubContext);
        private PetHomeSignals _petHomeSignals;
        private PetHomeScreenSignals _screenSignals;
        private GameplaySignals _gameplaySignals;
        private MainScreenSignals _mainScreenSignals;

        public override void Setup()
        {
            base.Setup();
            _petHomeSignals = InjectionBinderCrossContext.GetInstance<PetHomeSignals>();
            _screenSignals = InjectionBinderCrossContext.GetInstance<PetHomeScreenSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            IncomingSignals();
            OutgoingSignals();
        }

        private void IncomingSignals()
        {
            _mainScreenSignals.Outgoing.PetHomeClicked.Connect(_petHomeSignals.Incoming.Open, GROUP);
            _screenSignals.Outgoing.PickClicked.Connect(_petHomeSignals.Incoming.Pick, GROUP);
            _screenSignals.Outgoing.Placed.Connect(_petHomeSignals.Incoming.Place, GROUP);
            _screenSignals.Outgoing.Stored.Connect(_petHomeSignals.Incoming.Store, GROUP);
            _screenSignals.Outgoing.Ate.Connect(_petHomeSignals.Incoming.Eat, GROUP);
            _screenSignals.Outgoing.RefillClicked.Connect(_petHomeSignals.Incoming.Refill, GROUP);
            _screenSignals.Outgoing.Cared.Connect(_petHomeSignals.Incoming.Care, GROUP);
            _gameplaySignals.Outgoing.CoinsSpent.Connect(_petHomeSignals.Incoming.CoinsAnswered, GROUP);
        }

        private void OutgoingSignals()
        {
            _petHomeSignals.Outgoing.Opened.Connect(_screenSignals.Incoming.Open, GROUP);
            _petHomeSignals.Outgoing.Changed.Connect(_screenSignals.Incoming.Apply, GROUP);
            _petHomeSignals.Outgoing.NotBought.Connect(_screenSignals.Incoming.NotBought, GROUP);
            _petHomeSignals.Outgoing.CoinsRequested.Connect(_gameplaySignals.Incoming.SpendCoins, GROUP);
            _petHomeSignals.Outgoing.GrantCoins.Connect(_gameplaySignals.Incoming.GrantCoins, GROUP);
            _petHomeSignals.Outgoing.Rewarded.Connect(_screenSignals.Incoming.Rewarded, GROUP);
            _gameplaySignals.Outgoing.CoinsChanged.Connect(_screenSignals.Incoming.ApplyCoins, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
