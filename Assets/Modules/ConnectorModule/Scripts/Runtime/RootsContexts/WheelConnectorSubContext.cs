using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.Signals;
using Modules.GameplayModule.WheelScreenModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The prize wheel popup: the main menu's wheel button opens it, Gameplay's daily events fill it and Gameplay's
    /// spin turns it, and its buttons ask Gameplay for a spin, or for an ad that gives one.
    /// </summary>
    public class WheelConnectorSubContext : Context
    {
        private const string GROUP = nameof(WheelConnectorSubContext);
        private WheelScreenSignals _wheelSignals;
        private GameplaySignals _gameplaySignals;
        private MainScreenSignals _mainScreenSignals;

        public override void Setup()
        {
            base.Setup();
            _wheelSignals = InjectionBinderCrossContext.GetInstance<WheelScreenSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            IncomingSignals();
            OutgoingSignals();
        }

        private void IncomingSignals()
        {
            _mainScreenSignals.Outgoing.WheelClicked.Connect(_wheelSignals.Incoming.OpenWheelScreen, GROUP);
            _gameplaySignals.Outgoing.EventsChanged.Connect(_wheelSignals.Incoming.ApplyEvents, GROUP);
            _gameplaySignals.Outgoing.WheelSpun.Connect(_wheelSignals.Incoming.WheelSpun, GROUP);
        }

        private void OutgoingSignals()
        {
            _wheelSignals.Outgoing.EventsRequested.Connect(_gameplaySignals.Incoming.RequestEvents, GROUP);
            _wheelSignals.Outgoing.SpinClicked.Connect(_gameplaySignals.Incoming.SpinWheel, GROUP);
            _wheelSignals.Outgoing.AdSpinClicked.Connect(_gameplaySignals.Incoming.WatchAdForWheelSpin, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
