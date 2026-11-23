using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PetHomeModule.Models;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using Modules.PetHomeModule.Signals;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>An item dragged to a new spot stands there.</summary>
    internal sealed class PlacePetHomeItemCommand : Command
    {
        [Inject] private IPetHomeModel _model { get; set; }
        [SignalParam] private PlacedItemSVO _placed { get; set; }

        public override void Execute() => _model.Place(_placed);
    }
}
