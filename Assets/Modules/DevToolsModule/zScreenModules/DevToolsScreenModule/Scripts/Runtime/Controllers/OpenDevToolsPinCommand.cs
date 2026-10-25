using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.DevToolsModule.DevToolsScreenModule.Signals;
using Modules.DevToolsModule.DevToolsScreenModule.ViewsMediators;
using Modules.DevToolsModule.Services;
using Modules.LocalizationModule.Services;

namespace Modules.DevToolsModule.DevToolsScreenModule.Controllers
{
    /// <summary>
    /// The secret gesture was made. A device that may already open the panel opens it as usual; any
    /// other opens it behind the PIN pad, and nothing on it can be used until the PIN is accepted.
    /// </summary>
    internal sealed class OpenDevToolsPinCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private IDevToolsService _devTools { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [InjectSignal] private DevToolsScreenSignals _signals { get; set; }

        /// <summary>Opened as usual, opened behind the pad, came back null, threw - every way out resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            if (_devTools.PanelEnabled)
            {
                var signals = _signals;
                Release();
                signals.Incoming.OpenDevToolsScreen.Dispatch();
                return;
            }
            try
            {
                DevToolsScreenView screen = await _screenService.Open<DevToolsScreenView>().Show<DevToolsScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenDevToolsPinCommand - the screen did not open.");
                    Stop();
                    return;
                }
                screen.SetLocalizer(_localization.Get);
                screen.ShowPinPad(true);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenDevToolsPinCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
