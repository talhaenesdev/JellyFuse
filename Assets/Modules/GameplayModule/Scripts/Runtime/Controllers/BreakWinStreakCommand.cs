using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>A level left without being won - lost, or given up for another - ends the win streak.</summary>
    internal class BreakWinStreakCommand : Command
    {
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute()
        {
            // A duel is free: it neither needs nor costs a life, and leaves the run's win streak alone.
            if (_duel.Active) return;
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (progress == null || !_model.InPlay || _model.Status == PuzzleStatus.Won) return;
            progress.WinStreak = 0;
        }
    }
}
