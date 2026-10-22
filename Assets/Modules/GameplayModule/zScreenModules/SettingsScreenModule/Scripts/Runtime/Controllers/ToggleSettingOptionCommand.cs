using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.SettingsScreenModule.Models;
using Modules.GameplayModule.SettingsScreenModule.Signals;
using Modules.AudioModule.Services;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Services;
using Modules.NotificationModule.Services;

namespace Modules.GameplayModule.SettingsScreenModule.Controllers
{
    internal sealed class ToggleSettingOptionCommand : Command
    {
        [Inject] private ISettingsModel _model { get; set; }
        [Inject] private IHapticService _haptics { get; set; }
        [Inject] private IAudioService _audio { get; set; }
        [Inject] private INotificationService _notifications { get; set; }
        [InjectSignal] private SettingsScreenInternalSignals _signals { get; set; }
        [SignalParam] private SettingsOption _option { get; set; }

        public override void Execute()
        {
            bool enabled = _option switch
            {
                SettingsOption.Haptics => ToggleHaptics(),
                // The notification service keeps this preference: off, nothing scheduled is shown.
                SettingsOption.Notifications => ToggleNotifications(),
                // The audio service keeps these two preferences and acts on them at once.
                SettingsOption.Sounds => ToggleSounds(),
                SettingsOption.Music => ToggleMusic(),
                _ => _model.Toggle(_option),
            };

            _signals.OptionChanged.Dispatch(_option, enabled);
        }

        private bool ToggleSounds()
        {
            bool enabled = !_audio.SoundsEnabled;
            _audio.SetSoundsEnabled(enabled);
            return enabled;
        }
        private bool ToggleMusic()
        {
            bool enabled = !_audio.MusicEnabled;
            _audio.SetMusicEnabled(enabled);
            return enabled;
        }
        private bool ToggleNotifications()
        {
            bool enabled = !_notifications.Enabled;
            _notifications.SetEnabled(enabled);
            return enabled;
        }
        private bool ToggleHaptics()
        {
            _haptics.SetEnabled(!_haptics.Enabled);
            if (_haptics.Enabled) _haptics.Play(HapticPreset.Selection);
            return _haptics.Enabled;
        }
    }
}
