using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.LoginRewardsScreenModule.Models;
using Modules.GameplayModule.LoginRewardsScreenModule.ViewsMediators;
using Modules.LocalizationModule.Services;

namespace Modules.GameplayModule.LoginRewardsScreenModule.Controllers
{
    /// <summary>Opens the login rewards popup and fills it with the events last announced.</summary>
    internal class OpenLoginRewardsScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [Inject] private LoginRewardsModel _model { get; set; }

        /// <summary>Three ways out - opened, came back null, threw - and every one resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            try
            {
                var screen = await _screenService.Open<LoginRewardsScreenView>().Show<LoginRewardsScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenLoginRewardsScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }
                screen.SetLocalizer(_localization.Get);
                screen.ShowEvents(_model.Events);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenLoginRewardsScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
