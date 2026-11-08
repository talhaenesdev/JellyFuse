using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Models;
using Modules.AudioModule.Services;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    /// <summary>A cue at the pitch the caller asked for, its own jitter on top: a run of them can climb.</summary>
    internal sealed class PlayPitchedAudioCueCommand : Command
    {
        [Inject] private AudioModel _model { get; set; }
        [Inject] private IAudioPlayer _player { get; set; }
        [SignalParam] private AudioCue _cue { get; set; }
        [SignalParam] private float _pitch { get; set; }

        public override void Execute()
        {
            if (!_model.TryAccept(_cue, Time.realtimeSinceStartupAsDouble, out var entry)) return;
            var clip = entry.Clips[Random.Range(0, entry.Clips.Count)];
            float pitch = Mathf.Clamp(_pitch, .5f, 2f) * (1 + Random.Range(-entry.PitchJitter, entry.PitchJitter));
            _player.PlayOneShot(clip, entry.Volume * _model.Config.SoundsVolume, pitch);
        }
    }
}
