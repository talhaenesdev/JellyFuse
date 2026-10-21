using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Models;
using Modules.HapticModule.Services;
using UnityEngine;
namespace Modules.HapticModule.Controllers
{
    /// <summary>Plays a preset as a train of pulses for a length of time, when haptics are on and the preset is allowed.</summary>
    internal sealed class PlayHapticForCommand : Command
    {
        [Inject] private HapticModel _model { get; set; }
        [Inject] private IHapticDevice _device { get; set; }
        [SignalParam] private HapticPreset _preset { get; set; }
        [SignalParam] private float _seconds { get; set; }
        public override void Execute()
        {
            if (_seconds <= 0 || !Application.isFocused || !_model.TryAccept(_preset, Time.realtimeSinceStartupAsDouble, out var effect)) return;
#if UNITY_EDITOR
            if (_model.Config.LogInEditor) Debug.Log("Haptic: " + _preset + " -> " + effect + " for " + _seconds + "s");
#endif
            var feel = _model.EntryFor(effect) ?? _model.EntryFor(_preset);
            _device.PlayFor(effect, feel != null ? feel.Strength : .6f, feel != null ? feel.AndroidDurationMs : 30, feel != null ? feel.GapMs : 25, _seconds);
        }
    }
}
