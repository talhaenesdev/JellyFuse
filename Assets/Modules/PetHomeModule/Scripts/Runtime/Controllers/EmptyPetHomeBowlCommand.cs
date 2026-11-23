using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PetHomeModule.Models;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>The pet ate a bowl empty: it stays empty until the player fills it.</summary>
    internal sealed class EmptyPetHomeBowlCommand : Command
    {
        [Inject] private IPetHomeModel _model { get; set; }
        [SignalParam] private string _id { get; set; }

        public override void Execute() => _model.Empty(_id);
    }
}
