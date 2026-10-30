using FlowIoC.BaseModule.Signals;

namespace Modules.AnalyticsModule.Signals
{
    public class AnalyticsInternalSignals : ISignalHolder
    {
        public Signal StartCollecting = new();
    }
}
