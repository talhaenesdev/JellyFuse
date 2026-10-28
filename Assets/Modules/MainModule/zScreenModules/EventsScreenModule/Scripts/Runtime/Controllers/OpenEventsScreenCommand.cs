using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.MainModule.EventsScreenModule.ViewsMediators;

namespace Modules.MainModule.EventsScreenModule.Controllers
{
    internal sealed class OpenEventsScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }

        public override async void Execute()
        {
            Retain();
            try
            {
                EventsScreenView screen = await _screenService.Open<EventsScreenView>().Show<EventsScreenView>();
                if (screen == null)
                {
                    FlowLogger.LogError("OpenEventsScreenCommand - the events screen did not open.");
                    Stop();
                    return;
                }
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenEventsScreenCommand threw while opening the events screen: {exception}");
                Stop();
            }
        }
    }
}
