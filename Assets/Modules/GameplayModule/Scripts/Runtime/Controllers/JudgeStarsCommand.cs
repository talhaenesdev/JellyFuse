using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A level just won earns stars by the time left on its clock: three with more than half of it, two with more
    /// than a quarter, one otherwise. The best a level has been won with is kept. Judged once, before the win is paid.
    /// </summary>
    internal class JudgeStarsCommand : Command
    {
        private const float THREE_STARS = .5f, TWO_STARS = .25f;

        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var level = _model.Snapshot().Level;
            if (progress == null || level == null || _model.Status != PuzzleStatus.Won || _model.WinPaid) return;
            float left = level.Seconds > 0 ? Mathf.Clamp01(_model.RemainingSeconds / level.Seconds) : 1;
            int stars = left > THREE_STARS ? 3 : left > TWO_STARS ? 2 : 1;
            int index = _model.LevelIndex;
            while (progress.Stars.Count <= index) progress.Stars.Add(0);
            bool best = stars > progress.Stars[index];
            if (best) progress.Stars[index] = stars;
            _signals.Outgoing.StarsEarned.Dispatch(stars, best);
        }
    }
}
