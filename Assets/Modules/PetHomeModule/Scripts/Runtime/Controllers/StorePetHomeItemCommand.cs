using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PetHomeModule.Models;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using Modules.PetHomeModule.Signals;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>An item taken out of the room; the player keeps it and can put it back from the shop.</summary>
    internal sealed class StorePetHomeItemCommand : Command
    {
        [Inject] private IPetHomeModel _model { get; set; }
        [SignalParam] private string _id { get; set; }

        public override void Execute() => _model.Store(_id);
    }
}
