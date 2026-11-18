using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.LocalizationModule.Services;

namespace Modules.GameplayModule.DuelScreenModule.Controllers
{
    /// <summary>The player asked for a duel: the search panel shows while Gameplay finds an opponent.</summary>
    internal class OpenDuelSearchCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }

        public override async void Execute()
        {
            Retain();
            try
            {
                var screen = await DuelScreenOpenFunction.Get(_screenService, _shared, _localization);
                if (screen == null)
                {
                    FlowLogger.LogError("OpenDuelSearchCommand - the duel screen did not open.");
                    Stop();
                    return;
                }
                screen.ShowSearch();
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenDuelSearchCommand threw while opening the duel screen: {exception}");
                Stop();
            }
        }
    }
}
