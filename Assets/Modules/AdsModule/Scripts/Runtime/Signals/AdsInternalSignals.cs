using FlowIoC.BaseModule.Signals;

namespace Modules.AdsModule.Signals
{
    public class AdsInternalSignals : ISignalHolder
    {
        public Signal Initialize = new();
    }
}
