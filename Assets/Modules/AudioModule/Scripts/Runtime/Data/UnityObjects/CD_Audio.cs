using System.Collections.Generic;
using Modules.AudioModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.AudioModule.Data.UnityObjects
{
    /// <summary>
    /// Every sound the game makes: the clips of each cue, the loop of each music track, and the
    /// volumes over them. Filed in AudioServiceRoot's own Scriptables slot.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_Audio", menuName = "Audio/Config")]
    public sealed class CD_Audio : ScriptableObject
    {
        [Tooltip("Used only until the player changes them in the settings.")]
        public bool DefaultSoundsEnabled = true;
        public bool DefaultMusicEnabled = true;
        [Range(0, 1)] public float SoundsVolume = 1;
        [Range(0, 1)] public float MusicVolume = 1;
        [Tooltip("Seconds one music track takes to fade into the next.")]
        [Min(0)] public float MusicCrossfade = .8f;
        [Tooltip("Plays the Tap cue whenever an interactable UI button is released under the pointer.")]
        public bool UiClickSounds = true;
        [Tooltip("How many sound effects may sound at once; the oldest is cut when one more starts.")]
        [Range(1, 32)] public int Voices = 10;
        public List<AudioCueCVO> Cues = new();
        public List<MusicTrackCVO> Music = new();
    }
}
