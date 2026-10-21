using System;
using Modules.HapticModule.Enums;
using UnityEngine;
namespace Modules.HapticModule.Data.ValueObjects
{
    [Serializable]
    public sealed class HapticPresetCVO
    {
        public HapticPreset Preset;
        public HapticPreset Effect;
        public bool Enabled = true;
        [Min(0)] public float MinimumInterval = .08f;
        [Tooltip("How hard the motor runs, 0 to 1: the amplitude on Android, the impact intensity on iOS.")]
        [Range(0, 1)] public float Strength = .6f;
        [Tooltip("How long one pulse lasts on Android, in milliseconds. iOS plays its own fixed-length tap.")]
        [Range(5, 150)] public int AndroidDurationMs = 20;
        [Tooltip("For a preset held over time (Melt): the pause between two pulses, in milliseconds.")]
        [Range(0, 200)] public int GapMs = 25;
    }
}
