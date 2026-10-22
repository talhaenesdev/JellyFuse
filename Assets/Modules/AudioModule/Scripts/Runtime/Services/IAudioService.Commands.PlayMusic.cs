using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Enums;

namespace Modules.AudioModule.Services
{
    public partial interface IAudioService
    {
        public static partial class Commands
        {
            /// <summary>A step that switches the music: <c>.ToSequence&lt;IAudioService.Commands.PlayMusic&gt;(MusicTrack.Menu)</c>.</summary>
            public sealed class PlayMusic : Command<MusicTrack>
            {
                [Inject] private IAudioService _audio { get; set; }
                public override void Execute(MusicTrack track) => _audio.PlayMusic(track);
            }
        }
    }
}
