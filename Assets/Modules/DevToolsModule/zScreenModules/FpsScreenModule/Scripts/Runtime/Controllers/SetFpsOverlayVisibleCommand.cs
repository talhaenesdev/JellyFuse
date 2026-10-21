using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.DevToolsModule.FpsScreenModule.ViewsMediators;

namespace Modules.DevToolsModule.FpsScreenModule.Controllers
{
    /// <summary>Opens the readout, or hides it when it is up. Three ways out on open, each resolving the retain.</summary>
    internal sealed class SetFpsOverlayVisibleCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [SignalParam] private bool _visible { get; set; }

        public override async void Execute()
        {
            Retain();
            bool open = _screenService.Check.IsScreenActive<FpsScreenView>();
            if (!_visible)
            {
                if (open && _screenService.TryGet.Screen(out FpsScreenView shown) && shown != null) shown.Hide();
                Release();
                return;
            }
            if (open) { Release(); return; }
            try
            {
                var screen = await _screenService.Open<FpsScreenView>().Show<FpsScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("SetFpsOverlayVisibleCommand - the screen did not open.");
                    Stop();
                    return;
                }
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"SetFpsOverlayVisibleCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
