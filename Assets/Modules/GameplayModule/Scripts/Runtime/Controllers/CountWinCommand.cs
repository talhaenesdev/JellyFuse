using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Before a win is paid: a level just won adds to the win streak and to today's win quests. A win is
    /// counted once, however it is reached, the way it is paid once.
    /// </summary>
    internal class CountWinCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (progress == null || _model.Status != PuzzleStatus.Won || _model.WinPaid) return;
            progress.WinStreak++;
            EventsFunction.Advance(progress, QuestType.CompleteLevels, 1);
        }
    }
}
