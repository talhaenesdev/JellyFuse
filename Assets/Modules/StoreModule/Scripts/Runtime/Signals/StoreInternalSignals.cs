using FlowIoC.BaseModule.Signals;

namespace Modules.StoreModule.Signals
{
    public class StoreInternalSignals : ISignalHolder
    {
        public Signal Connect = new();
    }
}
