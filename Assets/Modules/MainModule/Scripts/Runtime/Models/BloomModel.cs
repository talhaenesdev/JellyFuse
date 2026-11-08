using UnityEngine;

namespace Modules.MainModule.Models
{
    /// <summary>
    /// Whether this device keeps Bloom, and the frame times that decide it. The frames are counted in windows of
    /// WINDOW_SECONDS after a GRACE_SECONDS settle; SLOW_WINDOWS slow windows in a row switch Bloom off, and the
    /// device remembers it, so a phone too slow for it never pays for it again.
    /// </summary>
    public sealed class BloomModel
    {
        private const string OFF_PREF = "Graphics.BloomOff";
        public const float GRACE_SECONDS = 5f;
        public const float WINDOW_SECONDS = 2f;
        public const int SLOW_WINDOWS = 2;
        /// <summary>A window is slow below this share of the target frame rate.</summary>
        public const float SLOW_SHARE = .8f;

        public bool IsTicking { get; set; }
        public bool IsHooked { get; set; }
        public bool Off { get; private set; } = PlayerPrefs.GetInt(OFF_PREF, 0) == 1;

        public float Watched { get; private set; }
        public float WindowTime { get; private set; }
        public int WindowFrames { get; private set; }
        public int SlowWindowsInRow { get; set; }

        public void AddFrame(float seconds)
        {
            Watched += seconds;
            if (Watched < GRACE_SECONDS) return;
            WindowTime += seconds;
            WindowFrames++;
        }

        public bool WindowFull => WindowTime >= WINDOW_SECONDS;
        public float WindowFps => WindowTime > 0 ? WindowFrames / WindowTime : 0;

        public void StartWindow()
        {
            WindowTime = 0;
            WindowFrames = 0;
        }

        public void SwitchOff()
        {
            Off = true;
            PlayerPrefs.SetInt(OFF_PREF, 1);
            PlayerPrefs.Save();
        }
    }
}
