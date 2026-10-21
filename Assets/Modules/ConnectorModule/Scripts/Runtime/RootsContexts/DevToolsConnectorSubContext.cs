using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.DevToolsModule.Constants;
using Modules.DevToolsModule.DevToolsScreenModule.Signals;
using Modules.DevToolsModule.FpsScreenModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;
using Modules.GameplayModule.GameplayScreenModule.Signals;
using Modules.GameplayModule.Shared.Constants;
using Modules.GameplayModule.Signals;
using Modules.MainModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The developer panel: three taps on the main menu's coin icon or on a level's label open it, its
    /// FPS switch shows the readout, and its tuning page drags the gameplay screen's jelly feel.
    /// </summary>
    public class DevToolsConnectorSubContext : Context
    {
        private const string GROUP = nameof(DevToolsConnectorSubContext);
        private DevToolsScreenSignals _devToolsSignals;
        private MainScreenSignals _mainScreenSignals;
        private GameplayScreenSignals _gameplayScreenSignals;
        private FpsScreenSignals _fpsSignals;
        private MainSignals _mainSignals;
        private GameplaySignals _gameplaySignals;
        public override void Setup()
        {
            base.Setup();
            _devToolsSignals = InjectionBinderCrossContext.GetInstance<DevToolsScreenSignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            _gameplayScreenSignals = InjectionBinderCrossContext.GetInstance<GameplayScreenSignals>();
            _fpsSignals = InjectionBinderCrossContext.GetInstance<FpsScreenSignals>();
            _mainSignals = InjectionBinderCrossContext.GetInstance<MainSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            IncomingSignals();
            OutgoingSignals();
        }
        private void IncomingSignals()
        {
            _mainScreenSignals.Outgoing.DevToolsRequested.Connect(_devToolsSignals.Incoming.OpenDevToolsScreen, GROUP);
            _gameplayScreenSignals.Outgoing.DevToolsRequested.Connect(_devToolsSignals.Incoming.OpenDevToolsScreen, GROUP);
            _gameplayScreenSignals.Outgoing.TuningSliders.Connect(_devToolsSignals.Incoming.ShowSliders, GROUP);
        }
        private void OutgoingSignals()
        {
            _devToolsSignals.Outgoing.Toggled(DevToolKeys.SHOW_FPS).Connect(_fpsSignals.Incoming.SetVisible, GROUP);
            _devToolsSignals.Outgoing.Toggled(DevToolKeys.UNCAPPED_FPS).Connect(_mainSignals.Incoming.ApplyFrameRate, GROUP);
            _devToolsSignals.Outgoing.Toggled(GameplayScreenDevToggles.TOP_BAR).Connect(_gameplayScreenSignals.Incoming.ShowTopBar, GROUP);
            _devToolsSignals.Outgoing.Toggled(GameplayScreenDevToggles.BOTTOM_BAR).Connect(_gameplayScreenSignals.Incoming.ShowBottomBar, GROUP);
            _devToolsSignals.Outgoing.SkipLevelRequested.Connect(_gameplaySignals.Incoming.SkipLevel, GROUP);
            _devToolsSignals.Outgoing.ResetBoostersRequested.Connect(_gameplaySignals.Incoming.ResetBoosters, GROUP);
            _devToolsSignals.Outgoing.RefillLivesRequested.Connect(_gameplaySignals.Incoming.ResetLives, GROUP);
            _devToolsSignals.Outgoing.ResetPlayerRequested.Connect(_mainSignals.Incoming.ResetPlayer, GROUP);
            _devToolsSignals.Outgoing.SlidersRequested.Connect(_gameplayScreenSignals.Incoming.SendTuningSliders, GROUP);
            _devToolsSignals.Outgoing.SliderChanged.Connect(_gameplayScreenSignals.Incoming.SetTuned, GROUP);
            _devToolsSignals.Outgoing.SlidersResetRequested.Connect(_gameplayScreenSignals.Incoming.ResetTuning, GROUP);
        }
        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
