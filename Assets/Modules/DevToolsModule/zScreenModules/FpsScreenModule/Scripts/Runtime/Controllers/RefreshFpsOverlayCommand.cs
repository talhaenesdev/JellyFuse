using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DevToolsModule.Constants;
using Modules.DevToolsModule.FpsScreenModule.Signals;
using Modules.DevToolsModule.Services;

namespace Modules.DevToolsModule.FpsScreenModule.Controllers
{
    /// <summary>At start: shows the readout when the switch was left on last time.</summary>
    internal sealed class RefreshFpsOverlayCommand : Command
    {
        [Inject] private IDevToolsService _devTools { get; set; }
        [InjectSignal] private FpsScreenSignals _signals { get; set; }

        public override void Execute() => _signals.Incoming.SetVisible.Dispatch(_devTools.IsOn(DevToolKeys.SHOW_FPS));
    }
}
