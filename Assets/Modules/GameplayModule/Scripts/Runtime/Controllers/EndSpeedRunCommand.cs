using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The level was left: any Speed Run in play is over, and the screen is told so.</summary>
    internal sealed class EndSpeedRunCommand : Command
    {
        [Inject] private ISpeedRunModel _run { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            if (!_run.Active) return;
            _run.End();
            _signals.Outgoing.SpeedRunChanged.Dispatch(new SpeedRunRVO());
        }
    }
}
