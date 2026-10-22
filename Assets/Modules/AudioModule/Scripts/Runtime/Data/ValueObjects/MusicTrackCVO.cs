using System;
using Modules.AudioModule.Enums;
using UnityEngine;

namespace Modules.AudioModule.Data.ValueObjects
{
    /// <summary>One piece of music: the looping clip a track plays, and how loud.</summary>
    [Serializable]
    public sealed class MusicTrackCVO
    {
        public MusicTrack Track;
        public AudioClip Clip;
        [Range(0, 1)] public float Volume = .45f;
    }
}
