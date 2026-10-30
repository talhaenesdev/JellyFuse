using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.Controllers
{
    internal class StartAnalyticsCommand : Command
    {
        [Inject] private IAnalyticsService _analytics { get; set; }

        public override void Execute() => _analytics.StartCollecting();
    }
}
