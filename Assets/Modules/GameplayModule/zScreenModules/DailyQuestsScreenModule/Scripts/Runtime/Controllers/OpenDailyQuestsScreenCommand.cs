using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.DailyQuestsScreenModule.Models;
using Modules.GameplayModule.DailyQuestsScreenModule.ViewsMediators;
using Modules.LocalizationModule.Services;

namespace Modules.GameplayModule.DailyQuestsScreenModule.Controllers
{
    /// <summary>Opens the quests popup and fills it with the events last announced.</summary>
    internal class OpenDailyQuestsScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [Inject] private DailyQuestsModel _model { get; set; }

        /// <summary>Three ways out - opened, came back null, threw - and every one resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            try
            {
                var screen = await _screenService.Open<DailyQuestsScreenView>().Show<DailyQuestsScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenDailyQuestsScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }
                screen.SetLocalizer(_localization.Get);
                screen.ShowEvents(_model.Events);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenDailyQuestsScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
