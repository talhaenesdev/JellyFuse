using System;
using System.Collections.Generic;
using Modules.AudioModule.Enums;
using UnityEngine;

namespace Modules.AudioModule.Data.ValueObjects
{
    /// <summary>How one cue sounds: one of its clips, picked at random, at this volume and pitch.</summary>
    [Serializable]
    public sealed class AudioCueCVO
    {
        public AudioCue Cue;
        public bool Enabled = true;
        [Tooltip("One is picked at random each time the cue plays, so a repeated sound does not tire the ear.")]
        public List<AudioClip> Clips = new();
        [Range(0, 1)] public float Volume = .8f;
        [Tooltip("The pitch is moved up or down by up to this much each time it plays (0.05 = five percent).")]
        [Range(0, .3f)] public float PitchJitter = .04f;
        [Tooltip("Seconds that must pass before the same cue plays again; a burst inside it plays once.")]
        [Min(0)] public float MinimumInterval = .03f;
    }
}
