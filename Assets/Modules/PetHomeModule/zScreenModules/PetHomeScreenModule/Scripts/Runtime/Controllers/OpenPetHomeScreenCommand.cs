using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.LocalizationModule.Services;
using Modules.PetHomeModule.PetHomeScreenModule.ViewsMediators;
using Modules.PetHomeModule.Shared.Data.ValueObjects;

namespace Modules.PetHomeModule.PetHomeScreenModule.Controllers
{
    /// <summary>Opens the pet home over the menu and fills it: texts, the room as it stands, the coins held.</summary>
    internal sealed class OpenPetHomeScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [SignalParam] private PetHomeRVO _home { get; set; }

        public override async void Execute()
        {
            Retain();
            var home = _home;
            try
            {
                PetHomeScreenView screen = await _screenService.Open<PetHomeScreenView>().ForceOpenAtFullLayer().Show<PetHomeScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenPetHomeScreenCommand - the pet home screen did not open.");
                    Stop();
                    return;
                }
                screen.ApplyTexts(_localization.Get);
                screen.Apply(home);
                var progress = _shared.GetScriptable<SD_JellyProgress>();
                screen.ApplyCoins(progress != null ? progress.Coins : 0);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenPetHomeScreenCommand threw while opening the pet home: {exception}");
                Stop();
            }
        }
    }
}
