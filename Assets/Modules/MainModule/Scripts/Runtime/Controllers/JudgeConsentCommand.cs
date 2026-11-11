using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.MainModule.Models;
using Modules.MainModule.Signals;

namespace Modules.MainModule.Controllers
{
    /// <summary>
    /// A player who has answered the consent question has ads and analytics started with that answer; one who
    /// has not is asked first, and they start once the answer comes back.
    /// </summary>
    internal sealed class JudgeConsentCommand : Command
    {
        [Inject] private ConsentModel _consent { get; set; }
        [InjectSignal] private MainSignals _signals { get; set; }
        [InjectSignal] private MainInternalSignals _internalSignals { get; set; }

        public override void Execute()
        {
            if (_consent.Answered) _internalSignals.StartConsentedServices.Dispatch(_consent.Accepted);
            else _signals.Outgoing.ConsentRequested.Dispatch();
        }
    }
}
