using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Data.UnityObjects;
using Modules.AudioModule.Data.ValueObjects;
using Modules.AudioModule.Enums;
using Modules.AudioModule.RootsContexts;
using UnityEngine;

namespace Modules.AudioModule.Models
{
    /// <summary>The sound config, the player's two preferences, and the rules for when a cue may sound.</summary>
    public sealed class AudioModel : IConstructable
    {
        public const string SOUNDS_KEY = "Audio.Sounds", MUSIC_KEY = "Audio.Music";

        [Inject(nameof(AudioServiceContext))] private GameObject _root { get; set; }

        private readonly Dictionary<AudioCue, double> _lastPlayed = new();

        public CD_Audio Config { get; private set; }
        public bool SoundsEnabled { get; private set; }
        public bool MusicEnabled { get; private set; }
        /// <summary>The track asked for last, playing or not: music switched back on resumes it.</summary>
        public MusicTrack Track { get; private set; }
        /// <summary>The UI click listener wants frames, and whether it is on the frame already.</summary>
        public bool IsListening { get; set; }
        public bool IsHooked { get; set; }
        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public void PostConstruct()
        {
            var config = _root.GetComponent<RootAdapter>().GetScriptable<CD_Audio>();
            Config = config;
            SoundsEnabled = PlayerPrefs.GetInt(SOUNDS_KEY, config == null || config.DefaultSoundsEnabled ? 1 : 0) == 1;
            MusicEnabled = PlayerPrefs.GetInt(MUSIC_KEY, config == null || config.DefaultMusicEnabled ? 1 : 0) == 1;
        }

        public void SetSoundsEnabled(bool enabled)
        {
            SoundsEnabled = enabled;
            PlayerPrefs.SetInt(SOUNDS_KEY, enabled ? 1 : 0);
            _lastPlayed.Clear();
        }

        public void SetMusicEnabled(bool enabled)
        {
            MusicEnabled = enabled;
            PlayerPrefs.SetInt(MUSIC_KEY, enabled ? 1 : 0);
        }

        public void SetTrack(MusicTrack track) => Track = track;

        /// <summary>The track's entry, when it is listed and has a clip.</summary>
        public MusicTrackCVO TrackEntry(MusicTrack track) => Config?.Music.Find(m => m != null && m.Track == track && m.Clip != null);

        /// <summary>
        /// Whether the cue sounds now: sounds are on, the cue is listed with a clip, and it has not
        /// sounded within its interval. Remembers the time when it does.
        /// </summary>
        public bool TryAccept(AudioCue cue, double now, out AudioCueCVO entry)
        {
            entry = null;
            if (!SoundsEnabled || Config == null) return false;
            entry = Config.Cues.Find(c => c != null && c.Cue == cue);
            if (entry == null || !entry.Enabled || entry.Clips.Count == 0) return false;
            if (_lastPlayed.TryGetValue(cue, out double last) && now - last < entry.MinimumInterval) return false;
            _lastPlayed[cue] = now;
            return true;
        }
    }
}
