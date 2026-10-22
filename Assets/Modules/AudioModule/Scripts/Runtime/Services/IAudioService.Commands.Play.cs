using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Enums;

namespace Modules.AudioModule.Services
{
    public partial interface IAudioService
    {
        public static partial class Commands
        {
            /// <summary>A step that plays a cue: <c>.ToSequence&lt;IAudioService.Commands.Play&gt;(AudioCue.Win)</c>.</summary>
            public sealed class Play : Command<AudioCue>
            {
                [Inject] private IAudioService _audio { get; set; }
                public override void Execute(AudioCue cue) => _audio.Play(cue);
            }
        }
    }
}
