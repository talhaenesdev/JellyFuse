using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.LocalizationModule.Services;
using Modules.MainModule.ConsentScreenModule.ViewsMediators;

namespace Modules.MainModule.ConsentScreenModule.Controllers
{
    /// <summary>Opens the consent question over whatever is on screen, in the game's language.</summary>
    internal sealed class OpenConsentScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }

        /// <summary>Three ways out - opened, came back null, threw - and every one resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            try
            {
                ConsentScreenView screen = await _screenService.Open<ConsentScreenView>().Show<ConsentScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenConsentScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }
                screen.SetLocalizer(_localization.Get);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenConsentScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
