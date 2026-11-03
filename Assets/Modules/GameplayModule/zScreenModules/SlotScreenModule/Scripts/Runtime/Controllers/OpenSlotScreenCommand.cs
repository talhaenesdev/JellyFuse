using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.SlotScreenModule.Models;
using Modules.GameplayModule.SlotScreenModule.ViewsMediators;
using Modules.LocalizationModule.Services;

namespace Modules.GameplayModule.SlotScreenModule.Controllers
{
    /// <summary>Opens the slot machine popup and fills it with the events last announced.</summary>
    internal class OpenSlotScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [Inject] private SlotModel _model { get; set; }

        /// <summary>Three ways out - opened, came back null, threw - and every one resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            try
            {
                var screen = await _screenService.Open<SlotScreenView>().Show<SlotScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenSlotScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }
                screen.SetLocalizer(_localization.Get);
                screen.ShowEvents(_model.Events);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenSlotScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
