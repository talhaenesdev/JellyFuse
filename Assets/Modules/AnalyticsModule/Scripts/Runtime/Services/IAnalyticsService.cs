using System.Collections.Generic;

namespace Modules.AnalyticsModule.Services
{
    /// <summary>Unity Analytics. Each event has to be defined in the dashboard's Event Manager.</summary>
    public interface IAnalyticsService
    {
        /// <summary>Starts sending. Called once as the game opens; recording before it is queued.</summary>
        void StartCollecting();

        /// <summary>Records one event with its parameters - ints, floats, bools or strings.</summary>
        void Record(string eventName, IReadOnlyDictionary<string, object> parameters = null);
    }
}
