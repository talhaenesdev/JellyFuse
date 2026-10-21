using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.GameplayScreenModule.Signals;
using Modules.GameplayModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    public class GameplayConnectorSubContext : Context
    {
        private const string GROUP = nameof(GameplayConnectorSubContext);
        private GameplaySignals _gameplaySignals;
        private GameplayScreenSignals _screenSignals;
        public override void Setup()
        {
            base.Setup();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            _screenSignals = InjectionBinderCrossContext.GetInstance<GameplayScreenSignals>();
            IncomingSignals();
            OutgoingSignals();
        }
        private void IncomingSignals()
        {
            _screenSignals.Outgoing.Start.Connect(_gameplaySignals.Incoming.Start, GROUP);
            _screenSignals.Outgoing.Drag.Connect(_gameplaySignals.Incoming.Drag, GROUP);
            _screenSignals.Outgoing.Undo.Connect(_gameplaySignals.Incoming.Undo, GROUP);
            _screenSignals.Outgoing.Closed.Connect(_gameplaySignals.Incoming.Quit, GROUP);
            _screenSignals.Outgoing.NextLevel.Connect(_gameplaySignals.Incoming.NextLevel, GROUP);
            _screenSignals.Outgoing.SkipLevel.Connect(_gameplaySignals.Incoming.SkipLevel, GROUP);
            _screenSignals.Outgoing.UseBooster.Connect(_gameplaySignals.Incoming.UseBooster, GROUP);
            _screenSignals.Outgoing.LivesRefreshRequested.Connect(_gameplaySignals.Incoming.RefreshLives, GROUP);
#if UNITY_EDITOR
            _screenSignals.Outgoing.ForceResult.Connect(_gameplaySignals.Incoming.ForceResult, GROUP);
#endif
        }
        private void OutgoingSignals()
        {
            _gameplaySignals.Outgoing.BoardChanged.Connect(_screenSignals.Incoming.ApplyBoard, GROUP);
            _gameplaySignals.Outgoing.TimeChanged.Connect(_screenSignals.Incoming.ApplyTime, GROUP);
            _gameplaySignals.Outgoing.LevelChanged.Connect(_screenSignals.Incoming.ApplyLevel, GROUP);
            _gameplaySignals.Outgoing.CoinsEarned.Connect(_screenSignals.Incoming.ApplyReward, GROUP);
            _gameplaySignals.Outgoing.CoinsChanged.Connect(_screenSignals.Incoming.ApplyCoins, GROUP);
            _gameplaySignals.Outgoing.BoostersChanged.Connect(_screenSignals.Incoming.ApplyBoosters, GROUP);
            _gameplaySignals.Outgoing.BoosterUsed.Connect(_screenSignals.Incoming.ApplyBoosterUsed, GROUP);
            _gameplaySignals.Outgoing.ClockFrozen.Connect(_screenSignals.Incoming.ApplyClockFrozen, GROUP);
            _gameplaySignals.Outgoing.LivesChanged.Connect(_screenSignals.Incoming.ApplyLives, GROUP);
            _gameplaySignals.Outgoing.OutOfLives.Connect(_screenSignals.Incoming.ApplyOutOfLives, GROUP);
            _gameplaySignals.Outgoing.TutorialChanged.Connect(_screenSignals.Incoming.ApplyTutorial, GROUP);
            _gameplaySignals.Outgoing.InfoRequested.Connect(_screenSignals.Incoming.ShowInfo, GROUP);
        }
        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
