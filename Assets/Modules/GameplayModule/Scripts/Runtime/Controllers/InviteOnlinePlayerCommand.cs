using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.DuelModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// The duel card asks a player whose game is open to race now. A yes arrives as a live start, which opens the race
    /// itself, and the flow ends here. Nobody open, a no, or no answer within the wait - the invite is withdrawn - and the
    /// flow goes on to race a recorded run or a bot.
    /// </summary>
    [LongRetain(30)]
    internal class InviteOnlinePlayerCommand : Command
    {
        private const float ANSWER_SECONDS = 10f;
        /// <summary>How far below the lower of the two players' levels the race level may be.</summary>
        private const int SPREAD = 15;

        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private IDuelService _duelService { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        public override async void Execute()
        {
            Retain();
            var duel = _duel;
            var service = _duelService;
            try
            {
                var levels = _shared.GetScriptable<CD_JellyLevels>();
                var events = _shared.GetScriptable<CD_Events>();
                var progress = _shared.GetScriptable<SD_JellyProgress>();
                int won = levels == null ? 0 : _model.LevelIndex >= 0 ? _model.LevelIndex : levels.StartIndex(progress);
                // Too few levels won, or offline: the next step decides.
                if (events == null || won < events.DuelMinLevel || !service.IsReady)
                {
                    Release();
                    return;
                }
                duel.Searching = true;
                var opponent = await service.FindLiveAsync(events.DuelMinLevel);
                if (opponent == null || duel.IsDeclined(opponent.PlayerId, Time.realtimeSinceStartup))
                {
                    Release();
                    return;
                }
                // Both have won it: the lower of the two levels, or a little under it.
                int top = opponent.Level > 0 ? Mathf.Min(won, opponent.Level) : won;
                int index = levels.PlayableFrom(Mathf.Max(0, top - 1 - Random.Range(0, SPREAD)));
                if (index < 0 || index >= won) index = levels.PlayableFrom(Mathf.Max(0, won - 1));
                string matchId = index < 0 ? null : await service.InviteLiveAsync(opponent.PlayerId, index + 1);
                if (string.IsNullOrEmpty(matchId))
                {
                    Release();
                    return;
                }
                FlowLogger.Log($"InviteOnlinePlayerCommand - invited {opponent.Name} to level {index + 1}, waiting {ANSWER_SECONDS:0}s.");
                duel.AwaitLive(matchId);
                float until = Time.realtimeSinceStartup + ANSWER_SECONDS;
                while (duel.AwaitingLive && Time.realtimeSinceStartup < until) await Task.Delay(250);
                if (duel.Live)
                {
                    // The yes started the race already.
                    Stop();
                    return;
                }
                if (duel.AwaitingLive)
                {
                    FlowLogger.Log($"InviteOnlinePlayerCommand - {opponent.Name} did not answer, invite withdrawn.");
                    duel.CancelledMatchId = matchId;
                    _ = service.CancelLiveAsync(matchId);
                    duel.StopAwaitingLive();
                }
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"InviteOnlinePlayerCommand threw while inviting: {exception}");
                duel.StopAwaitingLive();
                Release();
            }
        }
    }
}
