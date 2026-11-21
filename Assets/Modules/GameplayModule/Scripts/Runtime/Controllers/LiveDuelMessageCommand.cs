using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.DuelModule.Data.ValueObjects;
using Modules.DuelModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Signals;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A live duel message arrived. An invite is put in front of the player - or turned down at once while a level is
    /// played. A start chooses the level and opens it on both games together. Progress moves the other bar. Over ends
    /// the race for the one still playing: the other cleared first, or left.
    /// </summary>
    internal class LiveDuelMessageCommand : Command
    {
        private const float DECLINE_COOLDOWN_SECONDS = 300f;
        /// <summary>An answer faster than this came from the game itself, the player being busy.</summary>
        private const float BUSY_ANSWER_SECONDS = 4f;

        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private IDuelService _duelService { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [InjectSignal] private GameplayInternalSignals _internalSignals { get; set; }
        [SignalParam(0)] private string _type { get; set; }
        [SignalParam(1)] private string _body { get; set; }

        private sealed class Message
        {
            public string matchId;
            public string fromName;
            public string fromIcon;
            public string opponentName;
            public string opponentIcon;
            public string opponentId;
            public int level;
            public int delayMs;
            public long startAt;
            public int done;
            public int total;
            public bool youWon;
            public float opponentSeconds;
        }

        public override void Execute()
        {
            var message = string.IsNullOrEmpty(_body) ? null : JsonUtility.FromJson<Message>(_body);
            if (message == null || string.IsNullOrEmpty(message.matchId)) return;
            switch (_type)
            {
                case "duel.invite": Invite(message); break;
                case "duel.declined": Declined(message); break;
                case "duel.cancelled": Cancelled(message); break;
                case "duel.start": Start(message); break;
                case "duel.progress": Progress(message); break;
                case "duel.over": Over(message); break;
            }
        }

        private void Invite(Message message)
        {
            // Busy on a level or a duel: the sender hears no straight away rather than waiting. A duel already
            // judged is not busy - its result is on show, and a rematch invite lands there.
            // A finished duel's result is on show, lost or won: the board behind it no longer counts as being played.
            bool onResult = _duel.Active && _duel.Finished;
            if ((_model.InPlay && !onResult) || (_duel.Active && !_duel.Finished) || _duel.AwaitingLive || _duel.Searching)
            {
                _ = _duelService.AnswerLiveAsync(message.matchId, false);
                return;
            }
            _duel.LiveInvites[message.matchId] = new DuelChallengeVO { Id = message.matchId, FromName = message.fromName, FromIcon = message.fromIcon, Level = message.level };
            _signals.Outgoing.ChallengeReceived.Dispatch(new ChallengeInviteRVO(message.matchId, message.fromName, message.fromIcon, message.level));
        }

        private void Declined(Message message)
        {
            if (!_duel.AwaitingLive || _duel.LiveMatchId != message.matchId) return;
            // The duel card's own invite: a no only moves the search on to a bot.
            if (_duel.Searching)
            {
                _duel.StopAwaitingLive();
                return;
            }
            // Turned down by a person: that player is left alone for a while, so an invite cannot be sent again and
            // again. A no within seconds is the other game answering for a busy player, not a choice - no wait then.
            if (Time.realtimeSinceStartup - _duel.LiveSentAt > BUSY_ANSWER_SECONDS)
                _duel.MarkDeclined(_duel.ChallengeTargetId, Time.realtimeSinceStartup + DECLINE_COOLDOWN_SECONDS);
            _duel.StopAwaitingLive();
            _signals.Outgoing.DuelUnavailable.Dispatch();
        }

        private void Cancelled(Message message)
        {
            if (!_duel.LiveInvites.Remove(message.matchId)) return;
            _signals.Outgoing.ChallengeWithdrawn.Dispatch(message.matchId);
        }

        private void Start(Message message)
        {
            // Withdrawn while the yes was on its way: the other game hears this one left, and nothing starts here.
            if (message.matchId == _duel.CancelledMatchId)
            {
                _ = _duelService.FinishLiveAsync(message.matchId, -1);
                return;
            }
            var levels = _shared.GetScriptable<CD_JellyLevels>();
            if (levels == null || levels.Get(message.level - 1) == null)
            {
                FlowLogger.LogError($"LiveDuelMessageCommand - live level {message.level} cannot be played.");
                _ = _duelService.FinishLiveAsync(message.matchId, -1);
                return;
            }
            _duel.LiveInvites.Remove(message.matchId);
            _duel.Searching = false;
            _model.Daily = false;
            // How far the rival is known to have got: the highest level raced with them, never just the last one -
            // each rematch picks below it, and taking the last level as their best would shrink the choice to one level.
            int rivalBest = message.level;
            var known = _duel.RematchTarget;
            if (known != null && known.PlayerId == message.opponentId) rivalBest = Mathf.Max(rivalBest, known.Level);
            _duel.Choose(message.level - 1, message.opponentName, message.opponentIcon, null);
            // Both games start at the moment the server named, not a delay counted from when each message landed.
            float countdown = message.delayMs / 1000f;
            if (message.startAt > 0)
            {
                // Read against the server's clock: a phone whose own clock is a second or two out still starts on time.
                float untilStart = (message.startAt - _duelService.ServerNowMs) / 1000f;
                if (untilStart > .5f && untilStart < 10f) countdown = untilStart;
            }
            _duel.StartLive(message.matchId, countdown);
            if (!string.IsNullOrEmpty(message.opponentId))
                _duel.SetRematchTarget(new DuelTargetVO(message.opponentId, message.opponentName, message.opponentIcon, rivalBest));
            _signals.Outgoing.DuelChosen.Dispatch();
        }

        private void Progress(Message message)
        {
            if (!_duel.Live || _duel.LiveMatchId != message.matchId || message.total <= 0) return;
            _signals.Outgoing.DuelOpponentProgress.Dispatch(Mathf.Clamp01(message.done / (float)message.total));
        }

        private void Over(Message message)
        {
            if (!_duel.Live || _duel.LiveMatchId != message.matchId || _duel.Finished) return;
            _duel.Finish();
            // The race is over with this board unfinished: its clock stops with it.
            _model.IsTicking = false;
            int coins = 0;
            if (message.youWon) coins = DuelRewardFunction.Pay(_shared.GetScriptable<CD_Events>(), _shared.GetScriptable<SD_JellyProgress>());
            _signals.Outgoing.DuelFinished.Dispatch(new DuelResultRVO(message.youWon, coins, -1, message.opponentSeconds, canRematch: _duel.RematchTarget != null));
            if (coins > 0) _internalSignals.PayWin.Dispatch(coins);
        }
    }
}
