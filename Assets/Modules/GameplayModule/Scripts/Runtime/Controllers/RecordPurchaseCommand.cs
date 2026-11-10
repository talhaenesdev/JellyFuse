using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.Services;
using Modules.GameplayModule.Models;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Records a product the store says was paid for. The price lives in the store, which reports revenue itself.</summary>
    internal sealed class RecordPurchaseCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IAnalyticsService _analytics { get; set; }
        [SignalParam] private string _productId { get; set; }

        public override void Execute() =>
            _analytics.Record("purchase", new Dictionary<string, object> {{"product", _productId}, {"level", _model.LevelIndex + 1}});
    }
}
