using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PetHomeModule.Models;
using Modules.PetHomeModule.Shared.Enums;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>Something was done for the pet - a meal, a game, a pat, a nap - and it is that much happier.</summary>
    internal sealed class CarePetHomeCommand : Command
    {
        [Inject] private IPetHomeModel _model { get; set; }
        [SignalParam] private PetHomeCare _care { get; set; }

        public override void Execute() => _model.Care(_care);
    }
}
