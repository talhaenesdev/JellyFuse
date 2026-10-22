using Modules.AudioModule.Enums;

namespace Modules.AudioModule.Services
{
    /// <summary>The game's sound: effects by cue, music by track, and the player's two switches.</summary>
    public partial interface IAudioService
    {
        bool SoundsEnabled { get; }
        bool MusicEnabled { get; }
        void Play(AudioCue cue);
        /// <summary>Fades over to the track and loops it; asking for the one already playing changes nothing.</summary>
        void PlayMusic(MusicTrack track);
        void SetSoundsEnabled(bool enabled);
        /// <summary>Off fades the music out; on starts the track asked for last.</summary>
        void SetMusicEnabled(bool enabled);

        public static partial class Commands { }
    }
}
