using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.StatsScreenModule.Signals;
using Modules.ProfileModule.ProfileSelectionScreenModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    /// <summary>The player's stats popup: the profile screen's stats button opens it over that screen.</summary>
    public class StatsConnectorSubContext : Context
    {
        private const string GROUP = nameof(StatsConnectorSubContext);
        private StatsScreenSignals _statsSignals;
        private ProfileSelectionScreenSignals _profileSelectionSignals;

        public override void Setup()
        {
            base.Setup();
            _statsSignals = InjectionBinderCrossContext.GetInstance<StatsScreenSignals>();
            _profileSelectionSignals = InjectionBinderCrossContext.GetInstance<ProfileSelectionScreenSignals>();
            IncomingSignals();
        }

        private void IncomingSignals()
        {
            _profileSelectionSignals.Outgoing.StatsClicked.Connect(_statsSignals.Incoming.OpenStatsScreen, GROUP);
        }

        public override void DestroyContext()
        {
            SignalConnector.DisconnectGroup(GROUP);
            base.DestroyContext();
        }
    }
}
