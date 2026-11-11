using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.AnalyticsModule.Services
{
    public partial interface IAnalyticsService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Starts collection when the player agreed - the signal's payload. A refusal starts nothing, so the
            /// events recorded afterwards never leave the device.
            /// </summary>
            public class Start : Command
            {
                [Inject] private IAnalyticsService _analytics { get; set; }
                [SignalParam] private bool _consent { get; set; }

                public override void Execute()
                {
                    if (_consent) _analytics.StartCollecting();
                }
            }
        }
    }
}
