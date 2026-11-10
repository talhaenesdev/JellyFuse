using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Records a booster just used: which one, on which level.</summary>
    internal sealed class RecordBoosterUseCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IAnalyticsService _analytics { get; set; }
        [SignalParam] private BoosterType _type { get; set; }

        public override void Execute() =>
            _analytics.Record("booster_used", new Dictionary<string, object> {{"booster", _type.ToString()}, {"level", _model.LevelIndex + 1}});
    }
}
