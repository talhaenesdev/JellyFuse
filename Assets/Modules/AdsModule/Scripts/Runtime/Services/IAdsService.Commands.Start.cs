using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.AdsModule.Services
{
    public partial interface IAdsService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Starts LevelPlay with the player's answer to the consent question - the signal's payload: true
            /// lets the ads use personal data, false keeps them to non-personalised ones. Ads run either way.
            /// </summary>
            public class Start : Command
            {
                [Inject] private IAdsService _ads { get; set; }
                [SignalParam] private bool _consent { get; set; }

                public override void Execute() => _ads.Start(_consent);
            }
        }
    }
}
