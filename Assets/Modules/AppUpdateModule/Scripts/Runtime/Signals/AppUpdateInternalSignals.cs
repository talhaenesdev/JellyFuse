using FlowIoC.BaseModule.Signals;

namespace Modules.AppUpdateModule.Signals
{
    public class AppUpdateInternalSignals : ISignalHolder
    {
        public Signal CheckForUpdate = new();
    }
}
