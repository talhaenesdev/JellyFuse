using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.GameplayScreenModule.ViewsMediators;
using Modules.DevToolsModule.Services;
using Modules.GameplayModule.Shared.Constants;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.LocalizationModule.Services;

namespace Modules.GameplayModule.GameplayScreenModule.Controllers
{
    internal class OpenGameplayScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        // A Mediator injects nothing but its View, so the screen gets its texts from here, as it gets its presentation.
        [Inject] private ILocalizationService _localization { get; set; }
        // The purse the HUD shows top left, which the gameplay module files as shared.
        [Inject] private ISharedDataModel _shared { get; set; }
        // The developer switches that show or hide the top and bottom bars.
        [Inject] private IDevToolsService _devTools { get; set; }

        /// <summary>
        /// Three ways out, and every one of them resolves the retain: the screen opened, the screen
        /// came back null, and the await threw. A retain nobody resolves hangs the group for ever,
        /// with no timeout and nothing logged.
        /// </summary>
        public override async void Execute()
        {
            Retain();

            FlowLogger.Log("Execute - OpenGameplayScreenCommand");

            try
            {
                GameplayScreenView screen = await _screenService.Open<GameplayScreenView>().Show<GameplayScreenView>();

                if (screen == null)
                {
                    FlowLogger.LogError("OpenGameplayScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }

                screen.ConfigurePresentation();
                screen.SetLocalizer(_localization.Get);
                var progress = _shared.GetScriptable<SD_JellyProgress>();
                screen.ApplyCoins(progress != null ? progress.Coins : 0);
                screen.ShowTopBar(_devTools != null && _devTools.IsOn(GameplayScreenDevToggles.TOP_BAR));
                screen.ShowBottomBar(_devTools != null && _devTools.IsOn(GameplayScreenDevToggles.BOTTOM_BAR));
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenGameplayScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
