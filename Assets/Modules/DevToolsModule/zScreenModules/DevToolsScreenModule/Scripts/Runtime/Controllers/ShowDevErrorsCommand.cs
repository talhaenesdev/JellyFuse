using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Service;
using Modules.DevToolsModule.DevToolsScreenModule.ViewsMediators;
using Modules.DevToolsModule.Services;

namespace Modules.DevToolsModule.DevToolsScreenModule.Controllers
{
    /// <summary>Fills the open panel's errors page with the errors logged so far.</summary>
    internal sealed class ShowDevErrorsCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private IDevToolsService _devTools { get; set; }

        public override void Execute()
        {
            if (_screenService.TryGet.Screen(out DevToolsScreenView screen)) screen.ShowErrors(_devTools.Errors());
        }
    }
}
