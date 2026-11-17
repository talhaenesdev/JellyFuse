using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.StatsScreenModule.ViewsMediators;
using Modules.LocalizationModule.Services;

namespace Modules.GameplayModule.StatsScreenModule.Controllers
{
    /// <summary>Opens the stats popup and fills it with the player's totals from SD_JellyProgress.</summary>
    internal class OpenStatsScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }

        /// <summary>Not filed, opened, came back null, threw - every way out resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            try
            {
                var progress = _shared.GetScriptable<SD_JellyProgress>();
                if (progress == null)
                {
                    // The shared data model has already reported the missing filing.
                    Stop();
                    return;
                }
                var screen = await _screenService.Open<StatsScreenView>().Show<StatsScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenStatsScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }
                screen.SetLocalizer(_localization.Get);
                screen.ShowStats(progress.Stats);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenStatsScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
