using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Models;
using Modules.HapticModule.Services;
using UnityEngine;
namespace Modules.HapticModule.Controllers
{
    internal sealed class PlayHapticCommand : Command
    {
        [Inject] private HapticModel _model { get; set; }
        [Inject] private IHapticDevice _device { get; set; }
        [SignalParam] private HapticPreset _preset { get; set; }
        public override void Execute()
        {
            if (!Application.isFocused || !_model.TryAccept(_preset, Time.realtimeSinceStartupAsDouble, out var effect)) return;
#if UNITY_EDITOR
            if (_model.Config.LogInEditor) Debug.Log("Haptic: " + _preset + " -> " + effect);
#endif
            // How it feels comes from the entry of the effect that plays, after any remapping.
            var feel = _model.EntryFor(effect) ?? _model.EntryFor(_preset);
            _device.Play(effect, feel != null ? feel.Strength : .6f, feel != null ? feel.AndroidDurationMs : 20);
        }
    }
}
