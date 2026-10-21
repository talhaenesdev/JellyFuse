using FlowIoC.BaseModule.Signals;
using Modules.NotificationModule.Data.ValueObjects;
namespace Modules.NotificationModule.Signals
{
    internal class NotificationInternalSignals : ISignalHolder
    {
        public Signal RequestPermission = new();
        public Signal<bool> SetEnabled = new();
        public Signal<NotificationRequestVO> Schedule = new();
        public Signal<string> Cancel = new();
    }
}
