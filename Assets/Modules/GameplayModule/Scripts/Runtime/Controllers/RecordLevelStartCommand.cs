using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.Services;
using Modules.GameplayModule.Models;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Records the level that just started, for the dashboard's level funnel.</summary>
    public sealed class RecordLevelStartCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IAnalyticsService _analytics { get; set; }

        public override void Execute() =>
            _analytics.Record("level_start", new Dictionary<string, object> {{"level", _model.LevelIndex + 1}});
    }
}
