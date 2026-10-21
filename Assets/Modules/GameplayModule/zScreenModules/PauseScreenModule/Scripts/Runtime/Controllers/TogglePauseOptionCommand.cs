using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.PauseScreenModule.Models;
using Modules.GameplayModule.PauseScreenModule.Signals;
using Modules.GameplayModule.Shared.Enums;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Services;

namespace Modules.GameplayModule.PauseScreenModule.Controllers
{
    internal sealed class TogglePauseOptionCommand : Command
    {
        [Inject] private IPauseSettingsModel _model { get; set; }
        [Inject] private IHapticService _haptics { get; set; }
        [InjectSignal] private PauseScreenInternalSignals _signals { get; set; }
        [SignalParam] private SettingsOption _option { get; set; }

        public override void Execute()
        {
            bool enabled = _option == SettingsOption.Haptics
                ? ToggleHaptics()
                : _model.Toggle(_option);

            _signals.OptionChanged.Dispatch(_option, enabled);
        }

        private bool ToggleHaptics()
        {
            _haptics.SetEnabled(!_haptics.Enabled);
            if (_haptics.Enabled) _haptics.Play(HapticPreset.Selection);
            return _haptics.Enabled;
        }
    }
}
