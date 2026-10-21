using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Lets the sequence start the level only while a level is on screen; from the menu, a skip only picks it.</summary>
    internal class RequireLevelInPlayCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }

        public override void Execute()
        {
            if (_model.InPlay) return;
            Retain();
            Stop();
        }
    }
}
