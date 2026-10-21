using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Services;

namespace Modules.GameplayModule.GameplayScreenModule.Controllers
{
    /// <summary>A jelly is melting into its door: the phone trembles for as long as the melt is on screen.</summary>
    internal sealed class PlayMeltHapticCommand : Command
    {
        [Inject] private IHapticService _haptics { get; set; }
        [SignalParam] private float _seconds { get; set; }

        public override void Execute() => _haptics.PlayFor(HapticPreset.Melt, _seconds);
    }
}
