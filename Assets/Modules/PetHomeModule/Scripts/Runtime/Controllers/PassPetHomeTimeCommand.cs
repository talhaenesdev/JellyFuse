using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PetHomeModule.Models;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>The home opens after time away: the pet got a little less happy, and ate from its bowls.</summary>
    internal sealed class PassPetHomeTimeCommand : Command
    {
        [Inject] private IPetHomeModel _model { get; set; }

        public override void Execute() => _model.PassTime(DateTime.UtcNow);
    }
}
