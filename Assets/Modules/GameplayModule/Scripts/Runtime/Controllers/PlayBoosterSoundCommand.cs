using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Services;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The sound of the booster just applied; runs after ApplyBoosterCommand, which stops a use that did not happen.</summary>
    internal sealed class PlayBoosterSoundCommand : Command
    {
        [Inject] private IAudioService _audio { get; set; }
        [SignalParam] private BoosterType _type { get; set; }

        public override void Execute() => _audio.Play(_type switch
        {
            BoosterType.Rocket => AudioCue.Rocket,
            BoosterType.FreezeTime => AudioCue.Freeze,
            _ => AudioCue.Ufo,
        });
    }
}
