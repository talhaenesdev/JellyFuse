using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.LeaderboardModule.Enums;
using Modules.LocalizationModule.Services;
using Modules.MainModule.LeaderboardScreenModule.Models;
using Modules.MainModule.LeaderboardScreenModule.ViewsMediators;
using Modules.ProfileModule.Shared.Data.UnityObjects;

namespace Modules.MainModule.LeaderboardScreenModule.Controllers
{
    /// <summary>Opens the leaderboard on the all-time board; the next step fetches it.</summary>
    internal sealed class OpenLeaderboardScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [Inject] private LeaderboardScreenModel _model { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        public override async void Execute()
        {
            Retain();
            try
            {
                _model.Board = LeaderboardBoard.AllTime;
                LeaderboardScreenView screen = await _screenService.Open<LeaderboardScreenView>().Show<LeaderboardScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenLeaderboardScreenCommand - the leaderboard screen did not open.");
                    Stop();
                    return;
                }
                screen.SetLocalizer(_localization.Get);
                // The profile icons players pick; without the catalogue the rows go without pictures.
                screen.SetIcons(_shared.GetScriptable<CD_ProfileIcons>(), _shared.GetScriptable<CD_ProfileFrames>());
                screen.ShowLoading(LeaderboardBoard.AllTime);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenLeaderboardScreenCommand threw while opening the leaderboard: {exception}");
                Stop();
            }
        }
    }
}
