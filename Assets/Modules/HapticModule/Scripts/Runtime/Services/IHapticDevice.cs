using System;
using Modules.HapticModule.Enums;
namespace Modules.HapticModule.Services
{
    internal interface IHapticDevice : IDisposable
    {
        /// <summary>Plays the effect: strength 0..1 (motor amplitude on Android, intensity on iOS), length in milliseconds on Android.</summary>
        void Play(HapticPreset preset, float strength, int durationMs);
        /// <summary>Pulses of durationMs, gapMs apart, at the strength, until seconds have passed.</summary>
        void PlayFor(HapticPreset preset, float strength, int durationMs, int gapMs, float seconds);
        void Cancel();
    }
}
