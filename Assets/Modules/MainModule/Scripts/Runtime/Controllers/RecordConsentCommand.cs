using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.MainModule.Models;

namespace Modules.MainModule.Controllers
{
    /// <summary>Keeps the player's answer to the consent question, so it is asked once.</summary>
    internal sealed class RecordConsentCommand : Command
    {
        [Inject] private ConsentModel _consent { get; set; }
        [SignalParam] private bool _accepted { get; set; }

        public override void Execute() => _consent.Record(_accepted);
    }
}
