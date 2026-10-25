using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.ProfileModule.Models;

namespace Modules.ProfileModule.Controllers
{
    /// <summary>Reads the profile's icon and frame from SD_PlayerProfile again, after its values were replaced.</summary>
    internal sealed class ReloadProfileCommand : Command
    {
        [Inject] private IProfileModel _model { get; set; }

        public override void Execute() => _model.Reload();
    }
}
