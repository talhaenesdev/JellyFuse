using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.DuelModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// The duel level ended: won when the board was cleared faster than the opponent's run. A win pays CD_Events'
    /// duel coins for the day's first paid wins, and a cleared board - won or not - is filed for others to race.
    /// </summary>
    internal class FinishDuelCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private IDuelService _duelService { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [InjectSignal] private GameplayInternalSignals _internalSignals { get; set; }

        public override void Execute()
        {
            if (!_duel.Active || _duel.Finished || _model.Status == PuzzleStatus.Playing) return;
            _duel.Finish();
            bool cleared = _model.Status == PuzzleStatus.Won;
            float seconds = cleared ? Mathf.Max(0, Time.realtimeSinceStartup - _duel.RaceStart) : -1;
            bool won = cleared && seconds <= _duel.OpponentSeconds;

            // A live race: clearing first wins - had the other game cleared first, its message would have ended this race
            // already. Not cleared (time ran out, stuck) loses. Either way the other game is told.
            if (_duel.Live)
            {
                _ = _duelService.FinishLiveAsync(_duel.LiveMatchId, seconds);
                int prize = cleared ? DuelRewardFunction.Pay(_shared.GetScriptable<CD_Events>(), _shared.GetScriptable<SD_JellyProgress>()) : 0;
                _signals.Outgoing.DuelFinished.Dispatch(new DuelResultRVO(cleared, prize, seconds, -1));
                _model.WinPaid = true;
                _model.WinReward = prize;
                if (prize > 0) _internalSignals.PayWin.Dispatch(prize);
                return;
            }
            // Setting a challenge: nobody is raced, nothing is paid; a cleared run goes to the challenged player.
            if (!string.IsNullOrEmpty(_duel.ChallengeTargetId))
            {
                if (cleared) _ = _duelService.SendChallengeAsync(_duel.ChallengeTargetId, _duel.LevelIndex + 1, new List<float>(_duel.Exits));
                _signals.Outgoing.DuelFinished.Dispatch(new DuelResultRVO(cleared, 0, seconds, -1, cleared, _duel.OpponentName));
                if (cleared) { _model.WinPaid = true; _model.WinReward = 0; }
                return;
            }
            // Racing a challenge received: its sender is told how it went.
            if (!string.IsNullOrEmpty(_duel.ChallengeId)) _ = _duelService.AnswerChallengeAsync(_duel.ChallengeId, false, seconds);

            int coins = 0;
            var events = _shared.GetScriptable<CD_Events>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (won && events != null && progress != null)
            {
                int today = ShopDayFunction.Today();
                if (progress.DuelDay != today)
                {
                    progress.DuelDay = today;
                    progress.DuelWinsToday = 0;
                }
                if (progress.DuelWinsToday < events.DuelRewardedWins)
                {
                    progress.DuelWinsToday++;
                    coins = events.DuelRewardCoins;
                }
            }

            if (cleared)
            {
                // Fire and forget: nothing here waits for the server, so nothing is read after this line returns.
                _ = _duelService.SubmitAsync(_duel.LevelIndex + 1, new List<float>(_duel.Exits),
                    LevelFingerprintFunction.Of(_shared.GetScriptable<CD_JellyLevels>()?.Get(_duel.LevelIndex)));
            }
            _signals.Outgoing.DuelFinished.Dispatch(new DuelResultRVO(won, coins, seconds, _duel.OpponentSeconds));
            if (cleared)
            {
                _model.WinPaid = true;
                _model.WinReward = coins;
                if (coins > 0) _internalSignals.PayWin.Dispatch(coins);
            }
        }
    }
}
