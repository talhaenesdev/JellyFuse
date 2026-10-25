using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.CloudSyncModule.Signals;
using Modules.GameplayModule.Signals;
using Modules.MainModule.Signals;
using Modules.ProfileModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>
    /// The cloud copy of the player's progress: restored as a step of the boot, uploaded when a
    /// level ends and when the profile is saved, and the profile redrawn after a restore replaced it.
    /// </summary>
    public class CloudSyncConnectorSubContext : Context
    {
        private const string GROUP = nameof(CloudSyncConnectorSubContext);
        private CloudSyncSignals _cloudSyncSignals;
        private MainSignals _mainSignals;
        private GameplaySignals _gameplaySignals;
        private ProfileSignals _profileSignals;

        public override void Setup()
        {
            base.Setup();
            _cloudSyncSignals = InjectionBinderCrossContext.GetInstance<CloudSyncSignals>();
            _mainSignals = InjectionBinderCrossContext.GetInstance<MainSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            _profileSignals = InjectionBinderCrossContext.GetInstance<ProfileSignals>();
            IncomingSignals();
            OutgoingSignals();
        }

        private void IncomingSignals()
        {
            _mainSignals.Outgoing.BootStarted.Connect(_cloudSyncSignals.Incoming.Restore, GROUP);
            // Upload carries nothing: what to send is read from the saved assets, so the payloads are dropped.
            _gameplaySignals.Outgoing.LevelChanged.Connect((level, won) => _cloudSyncSignals.Incoming.Upload.Dispatch(), GROUP);
            _profileSignals.Outgoing.ProfileSaved.Connect(saved => _cloudSyncSignals.Incoming.Upload.Dispatch(), GROUP);
        }

        private void OutgoingSignals()
        {
            _cloudSyncSignals.Outgoing.Restored.Connect(_profileSignals.Incoming.ReloadProfile, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
