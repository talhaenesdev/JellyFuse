using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.Services;
using Modules.GameplayModule.Models;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Records each tutorial step the first time it is done, so the dashboard shows the step new players stop at.</summary>
    internal sealed class RecordTutorialStepCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IJellyTutorialModel _tutorial { get; set; }
        [Inject] private IAnalyticsService _analytics { get; set; }

        public override void Execute()
        {
            if (_tutorial.StepCount == 0 || _tutorial.StepIndex <= _tutorial.Reported) return;
            _tutorial.Reported = _tutorial.StepIndex;
            _analytics.Record("tutorial_step", new Dictionary<string, object>
            {
                {"level", _model.LevelIndex + 1}, {"step", _tutorial.StepIndex}, {"steps", _tutorial.StepCount}, {"finished", _tutorial.StepIndex >= _tutorial.StepCount}
            });
        }
    }
}
