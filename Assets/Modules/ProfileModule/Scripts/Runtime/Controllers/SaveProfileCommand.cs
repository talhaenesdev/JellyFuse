using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.ProfileModule.Models;
using Modules.ProfileModule.Shared.Data.ValueObjects;
using Modules.ProfileModule.Signals;

namespace Modules.ProfileModule.Controllers
{
    public sealed class SaveProfileCommand : Command
    {
        [Inject] private IProfileModel _model { get; set; }
        [InjectSignal] private ProfileSignals _signals { get; set; }
        [SignalParam] private ProfileEditVO _edit { get; set; }

        public override void Execute()
        {
            Retain();
            if (!_model.Save(_edit)) { Stop(); return; }
            ProfileSelectionRVO saved = _model.CreateSelection();
            _signals.Outgoing.IconChanged.Dispatch(_model.CurrentIcon);
            if (_model.CurrentFrame != null) _signals.Outgoing.FrameChanged.Dispatch(_model.CurrentFrame);
            _signals.Outgoing.ProfileSaved.Dispatch(saved);
            Release();
        }
    }
}
