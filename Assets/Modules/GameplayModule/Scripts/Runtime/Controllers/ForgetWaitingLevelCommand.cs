using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Enums;
using Modules.GameplayModule.Models;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The level was left: a start that waited for a life no longer waits, so a life bought from the menu starts nothing.</summary>
    internal class ForgetWaitingLevelCommand : Command
    {
        [Inject] private ILivesModel _lives { get; set; }

        public override void Execute() => _lives.WaitingStart = WaitingLevelStart.None;
    }
}
