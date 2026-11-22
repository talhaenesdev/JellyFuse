using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The level was left: an event level in play is over.</summary>
    internal sealed class EndEventPlayCommand : Command
    {
        [Inject] private IEventPlayModel _event { get; set; }

        public override void Execute() => _event.End();
    }
}
