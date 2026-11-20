using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.ProfileModule.ProfileSelectionScreenModule.ViewsMediators;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.ProfileModule.Shared.Data.ValueObjects;

namespace Modules.ProfileModule.ProfileSelectionScreenModule.Controllers
{
    public sealed class OpenProfileSelectionScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private Modules.LocalizationModule.Services.ILocalizationService _localization { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [SignalParam] private ProfileSelectionRVO _selection { get; set; }

        public override async void Execute()
        {
            Retain();
            try
            {
                ProfileSelectionScreenView screen = await _screenService.Open<ProfileSelectionScreenView>().Show<ProfileSelectionScreenView>();
                if (screen == null) { Stop(); return; }
                screen.SetLocalizer(_localization.Get);
                // The coins say which locked items can be bought; read where the game files them.
                var progress = _shared.GetScriptable<SD_JellyProgress>();
                screen.ApplyCoins(progress != null ? progress.Coins : 0);
                screen.ApplySelection(_selection);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenProfileSelectionScreenCommand failed: {exception}");
                Stop();
            }
        }
    }
}
