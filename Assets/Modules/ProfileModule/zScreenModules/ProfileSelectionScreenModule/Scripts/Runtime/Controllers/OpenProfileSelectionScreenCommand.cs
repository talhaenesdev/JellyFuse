using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.ProfileModule.ProfileSelectionScreenModule.ViewsMediators;
using Modules.ProfileModule.Shared.Data.ValueObjects;

namespace Modules.ProfileModule.ProfileSelectionScreenModule.Controllers
{
    public sealed class OpenProfileSelectionScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [SignalParam] private ProfileSelectionRVO _selection { get; set; }

        public override async void Execute()
        {
            Retain();
            try
            {
                ProfileSelectionScreenView screen = await _screenService.Open<ProfileSelectionScreenView>().Show<ProfileSelectionScreenView>();
                if (screen == null) { Stop(); return; }
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
