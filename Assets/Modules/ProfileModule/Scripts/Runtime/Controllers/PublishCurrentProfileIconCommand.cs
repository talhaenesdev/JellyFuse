using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.ProfileModule.Models;
using Modules.ProfileModule.Signals;

namespace Modules.ProfileModule.Controllers
{
    public sealed class PublishCurrentProfileIconCommand : Command
    {
        [Inject] private IProfileModel _model { get; set; }
        [InjectSignal] private ProfileSignals _signals { get; set; }

        public override void Execute()
        {
            if (_model.CurrentIcon != null) _signals.Outgoing.IconChanged.Dispatch(_model.CurrentIcon);
            if (_model.CurrentFrame != null) _signals.Outgoing.FrameChanged.Dispatch(_model.CurrentFrame);
        }
    }
}
