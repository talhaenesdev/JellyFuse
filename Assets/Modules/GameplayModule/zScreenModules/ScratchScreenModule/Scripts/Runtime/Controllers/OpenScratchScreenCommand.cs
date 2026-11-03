using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.ScratchScreenModule.Models;
using Modules.GameplayModule.ScratchScreenModule.ViewsMediators;
using Modules.LocalizationModule.Services;

namespace Modules.GameplayModule.ScratchScreenModule.Controllers
{
    /// <summary>Opens the scratch card popup and fills it with the events last announced.</summary>
    internal class OpenScratchScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [Inject] private ScratchModel _model { get; set; }

        /// <summary>Three ways out - opened, came back null, threw - and every one resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            try
            {
                var screen = await _screenService.Open<ScratchScreenView>().Show<ScratchScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenScratchScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }
                screen.SetLocalizer(_localization.Get);
                screen.ShowEvents(_model.Events);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenScratchScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
