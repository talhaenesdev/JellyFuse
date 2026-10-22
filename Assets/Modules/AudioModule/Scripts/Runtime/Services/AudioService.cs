using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Models;
using Modules.AudioModule.Signals;

namespace Modules.AudioModule.Services
{
    public sealed class AudioService : IAudioService
    {
        [Inject] private AudioModel _model { get; set; }
        [InjectSignal] private AudioInternalSignals _signals { get; set; }

        public bool SoundsEnabled => _model.SoundsEnabled;
        public bool MusicEnabled => _model.MusicEnabled;
        public void Play(AudioCue cue) => _signals.Play.Dispatch(cue);
        public void PlayMusic(MusicTrack track) => _signals.PlayMusic.Dispatch(track);
        public void SetSoundsEnabled(bool enabled) => _signals.SetSoundsEnabled.Dispatch(enabled);
        public void SetMusicEnabled(bool enabled) => _signals.SetMusicEnabled.Dispatch(enabled);
    }
}
