using System;
using Modules.HapticModule.Enums;
using UnityEngine;
namespace Modules.HapticModule.Services
{
    /// <summary>
    /// The phone's vibration motor. Android drives the Vibrator directly - a one-shot of the preset's
    /// length at its strength, a short pattern for Success and Failure - so the feel no longer depends
    /// on the system's touch-feedback setting. iOS plays an impact at the preset's intensity, or a
    /// notification for Success and Failure.
    /// </summary>
    internal sealed class HapticDevice : IHapticDevice
    {
        private bool _failed;
#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void JellyHapticPlay(int preset, float intensity);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void JellyHapticPlayFor(float intensity, int intervalMs, float seconds);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void JellyHapticCancel();
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
        private bool _ready;
        private int _sdk;
        private AndroidJavaObject _vibrator;
        private AndroidJavaClass _effects;
        private bool _amplitude;

        /// <summary>Finds the vibrator once; a phone without one leaves it null and plays nothing.</summary>
        private void Prepare()
        {
            if (_ready) return;
            _ready = true;
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            _sdk = version.GetStatic<int>("SDK_INT");
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            if (activity == null) return;
            if (_sdk >= 31)
            {
                using var manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
                _vibrator = manager?.Call<AndroidJavaObject>("getDefaultVibrator");
            }
            else _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            if (_vibrator == null || !_vibrator.Call<bool>("hasVibrator")) { _vibrator?.Dispose(); _vibrator = null; return; }
            if (_sdk >= 26)
            {
                _effects = new AndroidJavaClass("android.os.VibrationEffect");
                _amplitude = _vibrator.Call<bool>("hasAmplitudeControl");
            }
        }

        // Never called: Unity adds the VIBRATE permission to the Android manifest only when it finds
        // Handheld.Vibrate in the scripts, and the Vibrator calls above need that permission.
        [UnityEngine.Scripting.Preserve] private static void KeepVibratePermission() => Handheld.Vibrate();

        private int Amplitude(float strength) => _amplitude ? Mathf.Clamp(Mathf.RoundToInt(strength * 255), 1, 255) : -1;

        private void Vibrate(long[] timings, int[] amplitudes)
        {
            if (_sdk >= 26)
            {
                using var effect = timings.Length == 1
                    ? _effects.CallStatic<AndroidJavaObject>("createOneShot", timings[0], amplitudes[0])
                    : _effects.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1);
                _vibrator.Call("vibrate", effect);
            }
            else
            {
                long total = 0;
                foreach (var t in timings) total += t;
                _vibrator.Call("vibrate", total);
            }
        }
#endif

        public void Play(HapticPreset preset, float strength, int durationMs)
        {
            if (_failed) return;
            try
            {
                strength = Mathf.Clamp01(strength);
                durationMs = Mathf.Max(1, durationMs);
#if UNITY_ANDROID && !UNITY_EDITOR
                Prepare();
                if (_vibrator == null) return;
                int a = Amplitude(strength);
                switch (preset)
                {
                    // Two pulses, the second stronger: a little "ta-da".
                    case HapticPreset.Success:
                        Vibrate(new long[] { 0, durationMs, 70, durationMs }, new[] { 0, Amplitude(strength * .7f), 0, a });
                        break;
                    // Three quick, firm pulses.
                    case HapticPreset.Failure:
                        Vibrate(new long[] { 0, durationMs, 60, durationMs, 60, durationMs }, new[] { 0, a, 0, a, 0, a });
                        break;
                    default:
                        Vibrate(new long[] { durationMs }, new[] { a });
                        break;
                }
#elif UNITY_IOS && !UNITY_EDITOR
                JellyHapticPlay((int)preset, strength);
#endif
            }
            catch (Exception exception) { Fail(exception); }
        }

        public void PlayFor(HapticPreset preset, float strength, int durationMs, int gapMs, float seconds)
        {
            if (_failed) return;
            try
            {
                strength = Mathf.Clamp01(strength);
                durationMs = Mathf.Max(1, durationMs);
                gapMs = Mathf.Max(0, gapMs);
                int total = Mathf.Max(1, Mathf.RoundToInt(seconds * 1000));
#if UNITY_ANDROID && !UNITY_EDITOR
                Prepare();
                if (_vibrator == null) return;
                int a = Amplitude(strength);
                // A run of pulses that fills the time: on, off, on, off...
                int pulses = Mathf.Max(1, Mathf.CeilToInt(total / (float)(durationMs + gapMs)));
                var timings = new long[pulses * 2];
                var amplitudes = new int[pulses * 2];
                for (int i = 0; i < pulses; i++)
                {
                    timings[i * 2] = i == 0 ? 0 : gapMs; amplitudes[i * 2] = 0;
                    timings[i * 2 + 1] = durationMs; amplitudes[i * 2 + 1] = a;
                }
                if (gapMs == 0) Vibrate(new long[] { total }, new[] { a });
                else Vibrate(timings, amplitudes);
#elif UNITY_IOS && !UNITY_EDITOR
                JellyHapticPlayFor(strength, Mathf.Max(30, durationMs + gapMs), seconds);
#endif
            }
            catch (Exception exception) { Fail(exception); }
        }

        public void Cancel()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { JellyHapticCancel(); } catch (Exception exception) { Fail(exception); }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            try { _vibrator?.Call("cancel"); } catch (Exception exception) { Fail(exception); }
#endif
        }

        public void Dispose()
        {
            Cancel();
#if UNITY_ANDROID && !UNITY_EDITOR
            _vibrator?.Dispose(); _vibrator = null;
            _effects?.Dispose(); _effects = null;
#endif
        }

        private void Fail(Exception exception)
        {
            if (_failed) return;
            _failed = true;
            Debug.LogWarning("Haptic device unavailable; feedback disabled for this session. " + exception.Message);
        }
    }
}
