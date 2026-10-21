using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Enums;
namespace Modules.HapticModule.Services
{
    public partial interface IHapticService
    {
        public static partial class Commands
        {
            public sealed class Play : Command<HapticPreset>
            {
                [Inject] private IHapticService _haptics { get; set; }
                public override void Execute(HapticPreset preset) => _haptics.Play(preset);
            }
        }
    }
}
