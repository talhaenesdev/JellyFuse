using FlowIoC.BaseModule.Signals;
using Modules.AudioModule.Enums;

namespace Modules.AudioModule.Signals
{
    internal sealed class AudioInternalSignals : ISignalHolder
    {
        public Signal<AudioCue> Play = new(hideCommandLog: true);
        public Signal<MusicTrack> PlayMusic = new();
        public Signal<bool> SetSoundsEnabled = new();
        public Signal<bool> SetMusicEnabled = new();
        public Signal ListenForClicks = new();
        /// <summary>Each frame while the UI click listener is on.</summary>
        public Signal ClickTick = new(hideCommandLog: true);
    }
}
