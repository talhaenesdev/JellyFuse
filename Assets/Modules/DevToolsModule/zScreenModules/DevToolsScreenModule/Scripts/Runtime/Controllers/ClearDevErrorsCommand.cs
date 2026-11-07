using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DevToolsModule.Services;

namespace Modules.DevToolsModule.DevToolsScreenModule.Controllers
{
    /// <summary>Forgets the errors logged so far.</summary>
    internal sealed class ClearDevErrorsCommand : Command
    {
        [Inject] private IDevToolsService _devTools { get; set; }

        public override void Execute() => _devTools.ClearErrors();
    }
}
