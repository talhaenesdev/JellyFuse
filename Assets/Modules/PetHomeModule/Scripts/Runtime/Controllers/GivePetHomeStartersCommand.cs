using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PetHomeModule.Models;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using Modules.PetHomeModule.Signals;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>A new player's pet home gets its first room, bowl and bed - once.</summary>
    internal sealed class GivePetHomeStartersCommand : Command
    {
        [Inject] private IPetHomeModel _model { get; set; }

        public override void Execute() => _model.GiveStarters();
    }
}
