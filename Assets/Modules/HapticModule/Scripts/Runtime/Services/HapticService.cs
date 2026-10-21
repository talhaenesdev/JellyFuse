using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Models;
using Modules.HapticModule.Signals;
namespace Modules.HapticModule.Services
{
    public sealed class HapticService : IHapticService
    {
        [Inject] private HapticModel _model { get; set; }
        [InjectSignal] private HapticInternalSignals _signals { get; set; }
        public bool Enabled => _model.Enabled;
        public void Play(HapticPreset preset) => _signals.Play.Dispatch(preset);
        public void PlayFor(HapticPreset preset, float seconds) => _signals.PlayFor.Dispatch(preset, seconds);
        public void SetEnabled(bool enabled) => _signals.SetEnabled.Dispatch(enabled);
    }
}
