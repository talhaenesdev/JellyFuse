using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The duel level was left: the next start plays the run's own level again, and the race bar closes.</summary>
    internal class EndDuelCommand : Command
    {
        [Inject] private IDuelModel _duel { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            if (!_duel.Active) return;
            _duel.End();
            _signals.Outgoing.DuelEnded.Dispatch();
        }
    }
}
