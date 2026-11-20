using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DuelModule.Services;
using Modules.GameplayModule.Models;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// The player stopped waiting on the live invite they sent: the server withdraws it and the wait ends quietly. A
    /// start that crosses this on the way is ignored by the message handler.
    /// </summary>
    internal class CancelLiveInviteCommand : Command
    {
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private IDuelService _duelService { get; set; }

        public override void Execute()
        {
            if (!_duel.AwaitingLive || _duel.Live) return;
            FlowLogger.Log($"CancelLiveInviteCommand - invite {_duel.LiveMatchId} withdrawn.");
            _duel.CancelledMatchId = _duel.LiveMatchId;
            // Fire and forget: the menu does not wait for the server.
            _ = _duelService.CancelLiveAsync(_duel.LiveMatchId);
            _duel.StopAwaitingLive();
        }
    }
}
