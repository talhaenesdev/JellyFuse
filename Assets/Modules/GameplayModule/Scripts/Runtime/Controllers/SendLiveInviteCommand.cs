using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DuelModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// The level chosen, the challenged player is invited to race it now, and the step waits for the answer. Yes arrives
    /// as a live start and opens the race on both games; no, or nothing within the wait, tells the screen the duel is off.
    /// </summary>
    [LongRetain(45)]
    internal class SendLiveInviteCommand : Command
    {
        private const int WAIT_SECONDS = 32;

        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private IDuelService _duelService { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override async void Execute()
        {
            Retain();
            var duel = _duel;
            var signals = _signals;
            try
            {
                string target = duel.ChallengeTargetId;
                string matchId = await _duelService.InviteLiveAsync(target, duel.LevelIndex + 1);
                if (string.IsNullOrEmpty(matchId))
                {
                    duel.End();
                    signals.Outgoing.DuelUnavailable.Dispatch();
                    Stop();
                    return;
                }
                duel.AwaitLive(matchId);
                signals.Outgoing.LiveInviteSent.Dispatch(duel.OpponentName);
                for (int i = 0; i < WAIT_SECONDS * 2 && duel.AwaitingLive; i++) await Task.Delay(500);
                // Neither yes nor no came: the invite lapses here; a no already said so.
                if (duel.AwaitingLive && !duel.Live)
                {
                    duel.StopAwaitingLive();
                    signals.Outgoing.DuelUnavailable.Dispatch();
                }
                if (!duel.Live) duel.End();
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"SendLiveInviteCommand threw while inviting: {exception}");
                Stop();
            }
        }
    }
}
