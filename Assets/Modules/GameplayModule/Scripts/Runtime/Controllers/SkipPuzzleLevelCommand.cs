using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A developer move: to the next (+1) or previous (-1) playable level whether or not this one is
    /// won, wrapping round at either end. The Editor's N / B keys and the developer panel ask for it.
    /// </summary>
    internal class SkipPuzzleLevelCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [SignalParam] private int _direction { get; set; }

        public override void Execute()
        {
            var levels = _shared.GetScriptable<CD_JellyLevels>();
            // From the menu before any level was played, the skip counts from the level the player is on.
            if (_model.LevelIndex < 0) _model.LevelIndex = levels.StartIndex(_shared.GetScriptable<SD_JellyProgress>());
            int target = _direction < 0
                ? levels.PlayableBefore(_model.LevelIndex - 1) is var before && before >= 0 ? before : levels.PlayableBefore(levels.Count - 1)
                : levels.PlayableFrom(_model.LevelIndex + 1) is var after && after >= 0 ? after : levels.PlayableFrom(0);
            if (target < 0)
            {
                Retain();
                Stop();
                return;
            }
            FlowLogger.Log($"SkipPuzzleLevelCommand - jumping to level {target + 1} (developer skip).");
            _model.LevelIndex = target;
        }
    }
}
