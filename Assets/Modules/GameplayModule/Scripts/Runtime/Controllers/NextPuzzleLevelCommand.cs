using FlowIoC.BaseModule.Controller;
using FlowIoC.ConsoleModule;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Moves on to the next entry of CD_JellyLevels. Only a won level with an entry after it moves
    /// on; anything else stops the sequence, so the start that follows never runs.
    /// </summary>
    internal class NextPuzzleLevelCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }

        public override void Execute()
        {
            var levels = _shared.GetScriptable<CD_JellyLevels>();
            int next = levels.PlayableFrom(_model.LevelIndex + 1);
            if (_model.Status != PuzzleStatus.Won || next < 0)
            {
                Retain();
                Stop();
                return;
            }
            // Empty or broken entries in between are passed over; the Levels window marks them with a warning.
            for (int skipped = _model.LevelIndex + 1; skipped < next; skipped++)
                FlowLogger.LogWarning($"NextPuzzleLevelCommand - level {skipped + 1} is not playable and was skipped.");
            _model.LevelIndex = next;
        }
    }
}
