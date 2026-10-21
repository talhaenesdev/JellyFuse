using Modules.HapticModule.Enums;
namespace Modules.HapticModule.Services
{
    public partial interface IHapticService
    {
        bool Enabled { get; }
        void Play(HapticPreset preset);
        /// <summary>Keeps the preset going for this many seconds: a train of its pulses, as long as something lasts on screen.</summary>
        void PlayFor(HapticPreset preset, float seconds);
        void SetEnabled(bool enabled);
        public static partial class Commands { }
    }
}
