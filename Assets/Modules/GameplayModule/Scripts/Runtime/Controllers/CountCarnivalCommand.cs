using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// After a move: cells of today's carnival colour that left the board went through a door - a fuse keeps every
    /// cell, so the board holding fewer of them is the count. A duel's board is not the carnival's.
    /// </summary>
    internal sealed class CountCarnivalCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private PuzzleFeedbackModel _feedback { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute()
        {
            var before = _feedback.Before;
            var after = _model.Snapshot();
            if (_duel.Active || before == null || before.Level == null || before.Level != after.Level || after.Exited <= before.Exited) return;
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var config = _shared.GetScriptable<CD_Events>();
            if (progress == null || config == null) return;
            EventsFunction.RefreshDay(progress, config);
            var colour = (JellyColour)progress.CarnivalColour;
            int gone = Cells(before, colour) - Cells(after, colour);
            EventsFunction.AddCarnival(progress, config, gone);
        }

        private static int Cells(PuzzleSnapshotRVO board, JellyColour colour)
        {
            int cells = 0;
            foreach (var piece in board.Pieces) if (piece.Colour == colour) cells += piece.Cells.Count;
            return cells;
        }
    }
}
