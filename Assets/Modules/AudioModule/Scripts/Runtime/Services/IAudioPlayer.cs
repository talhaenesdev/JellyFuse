using System;
using UnityEngine;

namespace Modules.AudioModule.Services
{
    /// <summary>The AudioSources the service plays through; it decides nothing about what plays.</summary>
    public interface IAudioPlayer : IDisposable
    {
        void PlayOneShot(AudioClip clip, float volume, float pitch);
        /// <summary>Fades the music over to this clip; null fades it out.</summary>
        void PlayMusic(AudioClip clip, float volume, float fadeSeconds);
    }
}
