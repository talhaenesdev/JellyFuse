using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.Shared.Constants;
using Modules.GameplayModule.Signals;
using Modules.PurchaseModule.PurchaseScreenModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The purchase popup and the modules that sell through it. Each seller's offer opens and
    /// redraws the popup; Buy and the end of a countdown come back under the offer's key, so each
    /// seller is one line per signal and the popup never knows who sells.
    /// </summary>
    public class PurchaseConnectorSubContext : Context
    {
        private const string GROUP = nameof(PurchaseConnectorSubContext);
        private PurchaseScreenSignals _purchaseSignals;
        private GameplaySignals _gameplaySignals;

        public override void Setup()
        {
            base.Setup();
            _purchaseSignals = InjectionBinderCrossContext.GetInstance<PurchaseScreenSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            IncomingSignals();
            OutgoingSignals();
        }

        private void IncomingSignals()
        {
            // Lives, sold by Gameplay.
            _gameplaySignals.Outgoing.LivesOfferOpened.Connect(_purchaseSignals.Incoming.Open, GROUP);
            _gameplaySignals.Outgoing.LivesOfferChanged.Connect(_purchaseSignals.Incoming.ApplyOffer, GROUP);
            _gameplaySignals.Outgoing.LivesOfferFinished.Connect(_purchaseSignals.Incoming.Close, GROUP);
        }

        private void OutgoingSignals()
        {
            _purchaseSignals.Outgoing.Bought(GameplayOffers.LIVES).Connect(_gameplaySignals.Incoming.BuyLives, GROUP);
            _purchaseSignals.Outgoing.RefreshRequested(GameplayOffers.LIVES).Connect(_gameplaySignals.Incoming.RefreshLivesOffer, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
