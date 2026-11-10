using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// The daily challenge button: today's daily level is picked to play next, when there is one and it is
    /// not won yet. Otherwise the flow stops here and nothing opens.
    /// </summary>
    internal class ChooseDailyChallengeCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var events = _shared.GetScriptable<CD_Events>();
            if (progress == null || events == null || !EventsFunction.DailyChallengeReady(progress, events))
            {
                FlowLogger.Log("ChooseDailyChallengeCommand - no daily challenge to play today.");
                Retain();
                Stop();
                return;
            }
            _model.Daily = true;
        }
    }
}
