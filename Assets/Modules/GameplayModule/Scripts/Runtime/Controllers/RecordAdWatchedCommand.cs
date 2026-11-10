using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.Services;
using Modules.GameplayModule.Models;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Records a rewarded ad watched to its reward, with what it was watched for. Bound after the ad, so a skipped one never reaches it.</summary>
    internal sealed class RecordAdWatchedCommand : Command<string>
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IAnalyticsService _analytics { get; set; }

        public override void Execute(string placement) =>
            _analytics.Record("ad_watched", new Dictionary<string, object> {{"placement", placement}, {"level", _model.LevelIndex + 1}});
    }
}
