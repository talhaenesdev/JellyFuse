using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Services;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Services;
using Modules.PetHomeModule.PetHomeScreenModule.Enums;

namespace Modules.PetHomeModule.PetHomeScreenModule.Controllers
{
    /// <summary>
    /// A moment in the pet home is heard and felt: which of the game's sounds and which buzz each one takes. A ball's
    /// bounce is a soft bump, a meal finished a merge's pop, a ball brought back the delivery chime.
    /// </summary>
    internal sealed class PlayPetHomeFeelCommand : Command
    {
        [Inject] private IAudioService _audio { get; set; }
        [Inject] private IHapticService _haptics { get; set; }
        [SignalParam] private PetHomeFeel _feel { get; set; }

        public override void Execute()
        {
            switch (_feel)
            {
                case PetHomeFeel.Bounce:
                    _audio.Play(AudioCue.Bump, UnityEngine.Random.Range(.95f, 1.15f));
                    _haptics.Play(HapticPreset.Selection);
                    break;
                case PetHomeFeel.Place:
                    _audio.Play(AudioCue.Deliver);
                    _haptics.Play(HapticPreset.Light);
                    break;
                case PetHomeFeel.PickUp:
                    _audio.Play(AudioCue.Grab);
                    _haptics.Play(HapticPreset.Light);
                    break;
                case PetHomeFeel.Drop:
                    _audio.Play(AudioCue.Deliver);
                    _haptics.Play(HapticPreset.Medium);
                    break;
                case PetHomeFeel.Ate:
                    _audio.Play(AudioCue.Merge);
                    _haptics.Play(HapticPreset.Light);
                    break;
                case PetHomeFeel.Refill:
                    _audio.Play(AudioCue.Grab, 1.1f);
                    _haptics.Play(HapticPreset.Selection);
                    break;
                case PetHomeFeel.Petted:
                    _audio.Play(AudioCue.Tap, 1.2f);
                    _haptics.Play(HapticPreset.Selection);
                    break;
                case PetHomeFeel.Happy:
                    _audio.Play(AudioCue.Win);
                    _haptics.Play(HapticPreset.Success);
                    break;
            }
        }
    }
}
