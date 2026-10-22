using Modules.AudioModule.Services;
using Modules.HapticModule.Services;
using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.SettingsScreenModule.ViewsMediators;
using Modules.GameplayModule.SettingsScreenModule.Models;
using Modules.GameplayModule.Shared.Enums;
using Modules.LocalizationModule.Services;
using Modules.NotificationModule.Services;

namespace Modules.GameplayModule.SettingsScreenModule.Controllers
{
    /// <summary>Opens the settings over the game and fills it with the languages offered and the one in use.</summary>
    internal class OpenSettingsScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [Inject] private IHapticService _haptics { get; set; }
        [Inject] private IAudioService _audio { get; set; }
        [Inject] private INotificationService _notifications { get; set; }
        [Inject] private ISettingsModel _settings { get; set; }

        /// <summary>Three ways out - opened, came back null, threw - and every one resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            try
            {
                SettingsScreenView screen = await _screenService.Open<SettingsScreenView>().Show<SettingsScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenSettingsScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }
                screen.ShowLanguages(_localization.Languages, _localization.Language, _localization.Get);
                screen.ShowOption(SettingsOption.Notifications, _notifications.Enabled);
                screen.ShowOption(SettingsOption.Sounds, _audio.SoundsEnabled);
                screen.ShowOption(SettingsOption.Music, _audio.MusicEnabled);
                screen.ShowOption(SettingsOption.Haptics, _haptics.Enabled);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenSettingsScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
