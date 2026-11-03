using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.Signals;
using Modules.GameplayModule.SlotScreenModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The slot machine popup: the main menu's slot button opens it, Gameplay's daily events fill it and Gameplay's
    /// spin rolls its reels, and its buttons ask Gameplay for a spin, or for an ad that gives one.
    /// </summary>
    public class SlotConnectorSubContext : Context
    {
        private const string GROUP = nameof(SlotConnectorSubContext);
        private SlotScreenSignals _slotSignals;
        private GameplaySignals _gameplaySignals;
        private MainScreenSignals _mainScreenSignals;

        public override void Setup()
        {
            base.Setup();
            _slotSignals = InjectionBinderCrossContext.GetInstance<SlotScreenSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            IncomingSignals();
            OutgoingSignals();
        }

        private void IncomingSignals()
        {
            _mainScreenSignals.Outgoing.SlotClicked.Connect(_slotSignals.Incoming.OpenSlotScreen, GROUP);
            _gameplaySignals.Outgoing.EventsChanged.Connect(_slotSignals.Incoming.ApplyEvents, GROUP);
            _gameplaySignals.Outgoing.SlotSpun.Connect(_slotSignals.Incoming.SlotSpun, GROUP);
        }

        private void OutgoingSignals()
        {
            _slotSignals.Outgoing.EventsRequested.Connect(_gameplaySignals.Incoming.RequestEvents, GROUP);
            _slotSignals.Outgoing.SpinClicked.Connect(_gameplaySignals.Incoming.SpinSlot, GROUP);
            _slotSignals.Outgoing.AdSpinClicked.Connect(_gameplaySignals.Incoming.WatchAdForSlotSpin, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
