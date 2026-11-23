using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PetHomeModule.Models;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using Modules.PetHomeModule.Signals;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>The pet home is asked for: announced as it stands, for its screen to open on.</summary>
    internal sealed class OpenPetHomeCommand : Command
    {
        [Inject] private IPetHomeModel _model { get; set; }
        [InjectSignal] private PetHomeSignals _signals { get; set; }

        public override void Execute() => _signals.Outgoing.Opened.Dispatch(_model.Snapshot());
    }
}
