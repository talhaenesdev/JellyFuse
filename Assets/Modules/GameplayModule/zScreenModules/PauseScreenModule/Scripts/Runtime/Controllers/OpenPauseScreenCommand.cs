using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.PauseScreenModule.Models;
using Modules.GameplayModule.PauseScreenModule.ViewsMediators;
using Modules.GameplayModule.Shared.Enums;
using Modules.HapticModule.Services;
using Modules.LocalizationModule.Services;

namespace Modules.GameplayModule.PauseScreenModule.Controllers
{
    /// <summary>Opens the pause panel over the game and shows the switches as they stand.</summary>
    internal sealed class OpenPauseScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private IHapticService _haptics { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [Inject] private IPauseSettingsModel _settings { get; set; }

        /// <summary>Three ways out - opened, came back null, threw - and every one resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            try
            {
                PauseScreenView screen = await _screenService.Open<PauseScreenView>().Show<PauseScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenPauseScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }
                screen.SetLocalizer(_localization.Get);
                screen.ShowOption(SettingsOption.Sounds, _settings.IsEnabled(SettingsOption.Sounds));
                screen.ShowOption(SettingsOption.Music, _settings.IsEnabled(SettingsOption.Music));
                screen.ShowOption(SettingsOption.Haptics, _haptics.Enabled);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenPauseScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
