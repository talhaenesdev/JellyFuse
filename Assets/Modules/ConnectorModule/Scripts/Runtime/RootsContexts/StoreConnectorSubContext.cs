using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;
using Modules.StoreModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The real-money store: the shop page asks for prices and buys, the store answers with prices and
    /// with each product paid for, and Gameplay grants it.
    /// </summary>
    public class StoreConnectorSubContext : Context
    {
        private const string GROUP = nameof(StoreConnectorSubContext);
        private StoreSignals _storeSignals;
        private MainScreenSignals _mainScreenSignals;
        private GameplaySignals _gameplaySignals;

        public override void Setup()
        {
            base.Setup();
            _storeSignals = InjectionBinderCrossContext.GetInstance<StoreSignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            IncomingSignals();
            OutgoingSignals();
        }

        private void IncomingSignals()
        {
            _mainScreenSignals.Outgoing.ShopPricesRequested.Connect(_storeSignals.Incoming.RequestPrices, GROUP);
            _mainScreenSignals.Outgoing.ShopBuyClicked.Connect(_storeSignals.Incoming.Buy, GROUP);
        }

        private void OutgoingSignals()
        {
            _storeSignals.Outgoing.PricesChanged.Connect(_mainScreenSignals.Incoming.ApplyShopPrices, GROUP);
            _storeSignals.Outgoing.Purchased.Connect(_gameplaySignals.Incoming.GrantShopProduct, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
