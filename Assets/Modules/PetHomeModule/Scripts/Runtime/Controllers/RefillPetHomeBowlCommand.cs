using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PetHomeModule.Models;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>An empty bowl is filled again, for the pet to come and eat.</summary>
    internal sealed class RefillPetHomeBowlCommand : Command
    {
        [Inject] private IPetHomeModel _model { get; set; }
        [SignalParam] private string _id { get; set; }

        public override void Execute() => _model.Refill(_id);
    }
}
