using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.LocalizationModule.Services;

namespace Modules.GameplayModule.DuelScreenModule.Controllers
{
    /// <summary>
    /// The duel level is on the board: the countdown and the race bar, on the screen the search left up - or opened now,
    /// when the race is played again after a loss. The whole race is one event, so an open screen is fetched and filled.
    /// </summary>
    internal class StartDuelRaceScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [SignalParam] private DuelRaceRVO _race { get; set; }

        public override async void Execute()
        {
            Retain();
            var race = _race;
            try
            {
                var screen = await DuelScreenOpenFunction.Get(_screenService, _shared, _localization);
                if (screen == null)
                {
                    FlowLogger.LogError("StartDuelRaceScreenCommand - the duel screen did not open.");
                    Stop();
                    return;
                }
                screen.StartRace(race);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"StartDuelRaceScreenCommand threw while starting the race: {exception}");
                Stop();
            }
        }
    }
}
