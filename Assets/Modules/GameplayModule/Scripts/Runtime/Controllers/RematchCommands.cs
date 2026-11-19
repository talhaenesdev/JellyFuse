using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Play again on a finished live duel: the player raced is remembered to be invited once the level has been left.
    /// With nobody to invite, the duel screen is told the duel is off.
    /// </summary>
    internal class RequestRematchCommand : Command
    {
        [Inject] private IDuelModel _duel { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            if (_duel.RematchTarget == null || !_duel.Finished)
            {
                FlowLogger.Log("RequestRematchCommand - no live rival to invite again.");
                _signals.Outgoing.DuelUnavailable.Dispatch();
                Retain();
                Stop();
                return;
            }
            _duel.RematchRequested = true;
        }
    }

    /// <summary>The finished level was left: a rematch asked for is sent now, as a fresh invite to the same player.</summary>
    internal class SendRematchCommand : Command
    {
        [Inject] private IDuelModel _duel { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            if (!_duel.RematchRequested || _duel.RematchTarget == null) return;
            var target = _duel.RematchTarget;
            _duel.RematchRequested = false;
            FlowLogger.Log($"SendRematchCommand - inviting {target.Name} again.");
            _signals.Incoming.StartChallenge.Dispatch(target);
        }
    }
}
