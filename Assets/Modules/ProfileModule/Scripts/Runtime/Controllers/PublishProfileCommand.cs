using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LeaderboardModule.Services;
using Modules.ProfileModule.Models;

namespace Modules.ProfileModule.Controllers
{
    /// <summary>
    /// Sends the saved name, icon and frame to the leaderboard, so every board shows them. Offline, the step carries on:
    /// the profile is saved on the device either way, and the next save sends it again.
    /// </summary>
    internal sealed class PublishProfileCommand : Command
    {
        [Inject] private IProfileModel _model { get; set; }
        [Inject] private ILeaderboardService _leaderboard { get; set; }

        public override async void Execute()
        {
            Retain();
            try
            {
                var selection = _model.CreateSelection();
                await _leaderboard.SetProfileAsync(selection.PlayerName, selection.SelectedIconId, selection.SelectedFrameId);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"PublishProfileCommand threw while sending the profile: {exception}");
                Release();
            }
        }
    }
}
