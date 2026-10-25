using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DevToolsModule.DevToolsScreenModule.Signals;
using Modules.DevToolsModule.Services;

namespace Modules.DevToolsModule.DevToolsScreenModule.Controllers
{
    /// <summary>
    /// Checks the PIN typed. A match unlocks this device and the next step fills the panel; a miss says
    /// so on the pad and ends the sequence.
    /// </summary>
    internal sealed class UnlockDevToolsCommand : Command
    {
        [Inject] private IDevToolsService _devTools { get; set; }
        [InjectSignal] private DevToolsScreenInternalSignals _signals { get; set; }
        [SignalParam] private string _pin { get; set; }

        public override void Execute()
        {
            if (_devTools.TryUnlock(_pin)) return;
            _signals.PinRejected.Dispatch();
            Retain();
            Stop();
        }
    }
}
