using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>A level that starts brings its own tutorial, if it has one, at its first step.</summary>
    internal class StartTutorialCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _puzzle { get; set; }
        [Inject] private IJellyTutorialModel _tutorial { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var level = _shared.GetScriptable<CD_JellyLevels>().Get(_puzzle.LevelIndex);
            _tutorial.Begin(level != null ? level.Tutorial : new System.Collections.Generic.List<JellyTutorialStepCVO>());
            var board = _puzzle.Snapshot();
            JellyTutorialFunction.FindJelly(_tutorial, board);
            _signals.Outgoing.TutorialChanged.Dispatch(JellyTutorialFunction.Show(_tutorial, board));
        }
    }
}
