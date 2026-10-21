using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Tells the menu which level Play starts now.</summary>
    internal class AnnounceCurrentLevelCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute() => _signals.Outgoing.CurrentLevelChanged.Dispatch(_model.LevelIndex + 1);
    }
}
