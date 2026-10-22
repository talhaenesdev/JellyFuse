using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Models;
using Modules.AudioModule.Services;
using UnityEngine;

namespace Modules.AudioModule.Controllers
{
    internal sealed class PlayAudioCueCommand : Command
    {
        [Inject] private AudioModel _model { get; set; }
        [Inject] private IAudioPlayer _player { get; set; }
        [SignalParam] private AudioCue _cue { get; set; }

        public override void Execute()
        {
            if (!_model.TryAccept(_cue, Time.realtimeSinceStartupAsDouble, out var entry)) return;
            var clip = entry.Clips[Random.Range(0, entry.Clips.Count)];
            float pitch = 1 + Random.Range(-entry.PitchJitter, entry.PitchJitter);
            _player.PlayOneShot(clip, entry.Volume * _model.Config.SoundsVolume, pitch);
        }
    }
}
