using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.Services;
using Modules.GameplayModule.Models;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Records a level lost because the clock ran out.</summary>
    public sealed class RecordTimeOutCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IAnalyticsService _analytics { get; set; }

        public override void Execute() =>
            _analytics.Record("level_fail", new Dictionary<string, object>
            {
                {"level", _model.LevelIndex + 1},
                {"moves", _model.Snapshot().Moves},
                {"secondsLeft", 0},
                {"reason", "time"}
            });
    }
}
