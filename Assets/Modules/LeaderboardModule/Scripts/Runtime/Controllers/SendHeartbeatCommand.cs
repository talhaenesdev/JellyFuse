using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LeaderboardModule.Services;

namespace Modules.LeaderboardModule.Controllers
{
    /// <summary>
    /// Tells the server the game is open, so the boards show this player as active, then holds the step until the next
    /// beat is due. Not signed in yet, the beat is skipped and only the wait remains. A paused app sends nothing.
    /// </summary>
    [LongRetain(150)]
    internal class SendHeartbeatCommand : Command
    {
        private const int INTERVAL_MS = 90_000;
        private const int RETRY_MS = 5_000;

        [Inject] private ILeaderboardService _leaderboard { get; set; }

        public override async void Execute()
        {
            Retain();
            var leaderboard = _leaderboard;
            try
            {
                // Not signed in yet: try again soon, so the player shows as active from the first minute.
                bool sent = leaderboard.IsReady && await leaderboard.SendHeartbeatAsync();
                await Task.Delay(sent ? INTERVAL_MS : RETRY_MS);
                Release();
            }
            catch (Exception)
            {
                // A beat that fails is only a beat missed; the loop goes on.
                Release();
            }
        }
    }
}
