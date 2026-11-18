using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DuelModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// One turn of the challenge mail: reads what waits on the server, puts the newest unseen challenge and the outcomes of
    /// sent ones in front of the player - only while no level is on screen, else they wait for the next turn - and holds
    /// the step until the next turn is due.
    /// </summary>
    [LongRetain(60)]
    internal class PollChallengesCommand : Command
    {
        private const int INTERVAL_MS = 30_000;

        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private IDuelService _duelService { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override async void Execute()
        {
            Retain();
            try
            {
                var mail = _duelService.IsReady ? await _duelService.GetChallengesAsync() : null;
                if (mail != null)
                {
                    _duel.Pending.Clear();
                    foreach (var challenge in mail.Challenges) _duel.Pending[challenge.Id] = challenge;
                    foreach (var outcome in mail.Results) _duel.Outcomes.Enqueue(outcome);
                }
                if (!_model.InPlay)
                {
                    while (_duel.Outcomes.Count > 0)
                    {
                        var o = _duel.Outcomes.Dequeue();
                        _signals.Outgoing.ChallengeOutcome.Dispatch(new ChallengeOutcomeRVO(o.OpponentName, o.OpponentIcon, o.Declined, o.SenderWon, o.Seconds, o.OpponentSeconds));
                    }
                    Modules.DuelModule.Data.ValueObjects.DuelChallengeVO newest = null;
                    foreach (var challenge in _duel.Pending.Values)
                        if (!_duel.Shown.Contains(challenge.Id) && (newest == null || challenge.SentAt > newest.SentAt)) newest = challenge;
                    if (newest != null)
                    {
                        _duel.Shown.Add(newest.Id);
                        _signals.Outgoing.ChallengeReceived.Dispatch(new ChallengeInviteRVO(newest.Id, newest.FromName, newest.FromIcon, newest.Level));
                    }
                }
                await Task.Delay(INTERVAL_MS);
                Release();
            }
            catch (Exception)
            {
                // A turn that fails is a turn missed; the loop goes on.
                Release();
            }
        }
    }
}
