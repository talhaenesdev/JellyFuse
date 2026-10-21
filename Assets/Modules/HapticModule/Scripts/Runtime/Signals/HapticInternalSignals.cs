using FlowIoC.BaseModule.Signals;
using Modules.HapticModule.Enums;
namespace Modules.HapticModule.Signals
{
    internal sealed class HapticInternalSignals : ISignalHolder
    {
        public Signal<HapticPreset> Play = new(hideCommandLog: true);
        public Signal<HapticPreset, float> PlayFor = new(hideCommandLog: true);
        public Signal<bool> SetEnabled = new();
    }
}
