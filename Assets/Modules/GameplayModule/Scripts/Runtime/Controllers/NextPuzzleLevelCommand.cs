using FlowIoC.BaseModule.Controller;
using FlowIoC.ConsoleModule;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Signals;
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
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private IEventPlayModel _event { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            // After the daily challenge, Next goes back to the run's own level, without moving it on.
            if (_model.Daily && _model.Status == PuzzleStatus.Won) { _model.Daily = false; return; }
            // After a duel, Next goes back to the run's own level too.
            if (_duel.Active) { _duel.End(); _signals.Outgoing.DuelEnded.Dispatch(); return; }
            var levels = _shared.GetScriptable<CD_JellyLevels>();
            // After an event level, Next plays another one for the same event.
            if (_event.Active)
            {
                int again = EventLevelFunction.Pick(_event.Kind, levels, _shared.GetScriptable<SD_JellyProgress>(), _shared.GetScriptable<CD_Events>(), _event.LevelIndex);
                if (again >= 0) { _event.Begin(_event.Kind, again); return; }
                _event.End();
            }
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
