using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.DuelModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>A challenge was accepted: its level is the one to play, raced against the sender's run.</summary>
    internal class AcceptChallengeCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IDuelService _liveService { get; set; }
        [SignalParam] private string _id { get; set; }

        public override void Execute()
        {
            if (!string.IsNullOrEmpty(_id) && _duel.LiveInvites.Remove(_id))
            {
                _ = _liveService.AnswerLiveAsync(_id, true);
                Retain();
                Stop();
                return;
            }
            var levels = _shared.GetScriptable<CD_JellyLevels>();
            if (string.IsNullOrEmpty(_id) || !_duel.Pending.TryGetValue(_id, out var challenge) || levels == null || levels.Get(challenge.Level - 1) == null)
            {
                FlowLogger.Log($"AcceptChallengeCommand - challenge {_id} is gone or its level cannot be played.");
                Retain();
                Stop();
                return;
            }
            _duel.Pending.Remove(_id);
            _model.Daily = false;
            _duel.Choose(challenge.Level - 1, challenge.FromName, challenge.FromIcon, challenge.Exits);
            _duel.SetChallenge(_id);
        }
    }

    /// <summary>A challenge was declined: it leaves the inbox and its sender is told.</summary>
    internal class DeclineChallengeCommand : Command
    {
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private IDuelService _duelService { get; set; }
        [SignalParam] private string _id { get; set; }

        public override void Execute()
        {
            if (string.IsNullOrEmpty(_id)) return;
            if (_duel.LiveInvites.Remove(_id)) { _ = _duelService.AnswerLiveAsync(_id, false); return; }
            _duel.Pending.Remove(_id);
            // Fire and forget: the step does not wait for the server.
            _ = _duelService.AnswerChallengeAsync(_id, true, -1);
        }
    }
}
