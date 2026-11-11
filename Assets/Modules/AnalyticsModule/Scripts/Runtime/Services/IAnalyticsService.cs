using System.Collections.Generic;

namespace Modules.AnalyticsModule.Services
{
    /// <summary>Unity Analytics. Each event has to be defined in the dashboard's Event Manager.</summary>
    public partial interface IAnalyticsService
    {
        /// <summary>Starts sending. Called once the player has agreed; without it nothing leaves the device.</summary>
        void StartCollecting();

        /// <summary>Stops sending, when the player withdraws the agreement.</summary>
        void StopCollecting();

        /// <summary>Records one event with its parameters - ints, floats, bools or strings.</summary>
        void Record(string eventName, IReadOnlyDictionary<string, object> parameters = null);

        /// <summary>The steps this Service ships for a sequence to wait on.</summary>
        public static partial class Commands { }
    }
}
