using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PetHomeModule.Models;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using Modules.PetHomeModule.Signals;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>How the pet home stands now, after a change.</summary>
    internal sealed class AnnouncePetHomeCommand : Command
    {
        [Inject] private IPetHomeModel _model { get; set; }
        [InjectSignal] private PetHomeSignals _signals { get; set; }

        public override void Execute() => _signals.Outgoing.Changed.Dispatch(_model.Snapshot());
    }
}
