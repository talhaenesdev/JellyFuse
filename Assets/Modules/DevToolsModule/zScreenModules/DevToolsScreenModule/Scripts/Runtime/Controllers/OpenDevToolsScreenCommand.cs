using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.DevToolsModule.DevToolsScreenModule.ViewsMediators;
using Modules.DevToolsModule.Services;
using Modules.LocalizationModule.Services;

namespace Modules.DevToolsModule.DevToolsScreenModule.Controllers
{
    /// <summary>Opens the developer panel with one row per switch, each showing its value.</summary>
    internal sealed class OpenDevToolsScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private IDevToolsService _devTools { get; set; }
        // A Mediator injects nothing but its View, so the panel gets its texts from here.
        [Inject] private ILocalizationService _localization { get; set; }

        /// <summary>Three ways out - opened, came back null, threw - and every one resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            // A store build ships with the panel switched off: the taps that ask for it open nothing.
            if (!_devTools.PanelEnabled)
            {
                Stop();
                return;
            }
            try
            {
                DevToolsScreenView screen = await _screenService.Open<DevToolsScreenView>().Show<DevToolsScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenDevToolsScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }
                screen.SetLocalizer(_localization.Get);
                screen.ShowToggles(_devTools.Toggles);
                foreach (var toggle in _devTools.Toggles) screen.ShowToggle(toggle.Key, _devTools.IsOn(toggle.Key));
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenDevToolsScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
