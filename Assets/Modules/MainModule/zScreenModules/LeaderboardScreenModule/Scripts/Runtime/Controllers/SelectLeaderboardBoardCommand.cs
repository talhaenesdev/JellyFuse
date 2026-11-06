using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LeaderboardModule.Enums;
using Modules.MainModule.LeaderboardScreenModule.Models;

namespace Modules.MainModule.LeaderboardScreenModule.Controllers
{
    /// <summary>A tab picked: that board is the one shown, and the next step fetches it.</summary>
    internal sealed class SelectLeaderboardBoardCommand : Command
    {
        [Inject] private LeaderboardScreenModel _model { get; set; }
        [SignalParam] private LeaderboardBoard _board { get; set; }

        public override void Execute() => _model.Board = _board;
    }
}
