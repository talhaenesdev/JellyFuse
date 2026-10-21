using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.ProfileModule.Models;
using Modules.ProfileModule.Signals;

namespace Modules.ProfileModule.Controllers
{
    public sealed class OpenProfileSelectionCommand : Command
    {
        [Inject] private IProfileModel _model { get; set; }
        [InjectSignal] private ProfileSignals _signals { get; set; }

        public override void Execute() => _signals.Outgoing.SelectionRequested.Dispatch(_model.CreateSelection());
    }
}
