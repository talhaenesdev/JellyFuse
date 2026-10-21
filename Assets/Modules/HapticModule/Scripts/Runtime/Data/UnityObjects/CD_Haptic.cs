using System.Collections.Generic;
using Modules.HapticModule.Data.ValueObjects;
using Modules.HapticModule.Enums;
using UnityEngine;
namespace Modules.HapticModule.Data.UnityObjects
{
    [CreateAssetMenu(fileName = "CD_Haptic", menuName = "Haptic/Config")]
    public sealed class CD_Haptic : ScriptableObject
    {
        public bool DefaultEnabled = true;
        [Tooltip("Logs accepted effects in the Editor; no device vibration in the Editor.")]
        public bool LogInEditor;
        public List<HapticPresetCVO> Presets = new()
        {
            new() { Preset = HapticPreset.Selection, Effect = HapticPreset.Selection, Strength = .45f, AndroidDurationMs = 15 },
            new() { Preset = HapticPreset.Light, Effect = HapticPreset.Light, Strength = .55f, AndroidDurationMs = 20 },
            new() { Preset = HapticPreset.Medium, Effect = HapticPreset.Medium, Strength = .8f, AndroidDurationMs = 30 },
            new() { Preset = HapticPreset.Heavy, Effect = HapticPreset.Heavy, Strength = 1f, AndroidDurationMs = 45 },
            new() { Preset = HapticPreset.Success, Effect = HapticPreset.Success, MinimumInterval = .25f, Strength = .9f, AndroidDurationMs = 35 },
            new() { Preset = HapticPreset.Failure, Effect = HapticPreset.Failure, MinimumInterval = .25f, Strength = 1f, AndroidDurationMs = 40 },
            new() { Preset = HapticPreset.Melt, Effect = HapticPreset.Melt, MinimumInterval = 0f, Strength = .7f, AndroidDurationMs = 30, GapMs = 25 }
        };
    }
}
