using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.WheelScreenModule.Models;
using Modules.GameplayModule.WheelScreenModule.ViewsMediators;
using Modules.LocalizationModule.Services;

namespace Modules.GameplayModule.WheelScreenModule.Controllers
{
    /// <summary>Opens the prize wheel popup and fills it with the events last announced.</summary>
    internal class OpenWheelScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [Inject] private WheelModel _model { get; set; }

        /// <summary>Three ways out - opened, came back null, threw - and every one resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            try
            {
                var screen = await _screenService.Open<WheelScreenView>().Show<WheelScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenWheelScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }
                screen.SetLocalizer(_localization.Get);
                screen.ShowEvents(_model.Events);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenWheelScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
