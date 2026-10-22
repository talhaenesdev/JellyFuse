using System.Collections.Generic;
using DG.Tweening;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Models;
using Modules.AudioModule.RootsContexts;
using UnityEngine;

namespace Modules.AudioModule.Services
{
    /// <summary>
    /// A ring of AudioSources for effects and two for music, on a child of the Root so they live as
    /// long as it. Two music sources let one track fade out while the next fades in.
    /// </summary>
    public sealed class AudioPlayer : IAudioPlayer, IConstructable
    {
        [Inject(nameof(AudioServiceContext))] private GameObject _root { get; set; }
        [Inject] private AudioModel _model { get; set; }

        private readonly List<AudioSource> _voices = new();
        private readonly AudioSource[] _music = new AudioSource[2];
        private GameObject _host;
        private int _nextVoice, _activeMusic;

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public void PostConstruct()
        {
            _host = new GameObject("Audio");
            _host.transform.SetParent(_root.transform, false);
            int voices = _model.Config != null ? Mathf.Max(1, _model.Config.Voices) : 8;
            for (int i = 0; i < voices; i++) _voices.Add(Source(false));
            for (int i = 0; i < 2; i++) _music[i] = Source(true);
        }

        private AudioSource Source(bool loop)
        {
            var source = _host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0;
            return source;
        }

        public void PlayOneShot(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || _voices.Count == 0) return;
            var source = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Count;
            source.Stop();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }

        public void PlayMusic(AudioClip clip, float volume, float fadeSeconds)
        {
            var current = _music[_activeMusic];
            if (current == null) return;
            if (clip != null && current.clip == clip && current.isPlaying)
            {
                Fade(current, volume, fadeSeconds, false);
                return;
            }
            Fade(current, 0, fadeSeconds, true);
            if (clip == null) return;
            _activeMusic = 1 - _activeMusic;
            var next = _music[_activeMusic];
            next.clip = clip;
            next.volume = 0;
            next.Play();
            Fade(next, volume, fadeSeconds, false);
        }

        // DOTween's core API only, so no DOTween module assembly is needed.
        private static void Fade(AudioSource source, float to, float seconds, bool stopAtEnd)
        {
            DOTween.Kill(source);
            var tween = DOTween.To(() => source.volume, v => source.volume = v, to, seconds).SetTarget(source).SetUpdate(true);
            if (stopAtEnd) tween.OnComplete(source.Stop);
        }

        public void Dispose()
        {
            foreach (var source in _music) if (source != null) DOTween.Kill(source);
            if (_host != null) Object.Destroy(_host);
        }
    }
}
