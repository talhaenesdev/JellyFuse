using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DevToolsModule.DevToolsScreenModule.Signals;
using Modules.DevToolsModule.Services;

namespace Modules.DevToolsModule.DevToolsScreenModule.Controllers
{
    /// <summary>Flips a developer switch and tells the panel the value it now holds.</summary>
    internal sealed class SetDevToggleCommand : Command
    {
        [Inject] private IDevToolsService _devTools { get; set; }
        [InjectSignal] private DevToolsScreenInternalSignals _signals { get; set; }
        [InjectSignal] private DevToolsScreenSignals _publicSignals { get; set; }
        [SignalParam] private string _key { get; set; }
        [SignalParam] private bool _on { get; set; }

        public override void Execute()
        {
            _devTools.Set(_key, _on);
            bool on = _devTools.IsOn(_key);
            _signals.ToggleChanged.Dispatch(_key, on);
            _publicSignals.Outgoing.Toggled(_key).Dispatch(on);
        }
    }
}
