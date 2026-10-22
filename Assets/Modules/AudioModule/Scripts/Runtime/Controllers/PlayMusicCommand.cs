using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Models;
using Modules.AudioModule.Services;

namespace Modules.AudioModule.Controllers
{
    /// <summary>Remembers the track asked for and, with music on, fades over to it.</summary>
    internal sealed class PlayMusicCommand : Command
    {
        [Inject] private AudioModel _model { get; set; }
        [Inject] private IAudioPlayer _player { get; set; }
        [SignalParam] private MusicTrack _track { get; set; }

        public override void Execute()
        {
            _model.SetTrack(_track);
            ApplyMusic(_model, _player);
        }

        /// <summary>What should be playing now: the remembered track with music on, silence otherwise.</summary>
        internal static void ApplyMusic(AudioModel model, IAudioPlayer player)
        {
            var entry = model.MusicEnabled ? model.TrackEntry(model.Track) : null;
            float fade = model.Config != null ? model.Config.MusicCrossfade : .5f;
            float volume = entry != null ? entry.Volume * model.Config.MusicVolume : 0;
            player.PlayMusic(entry?.Clip, volume, fade);
        }
    }
}
