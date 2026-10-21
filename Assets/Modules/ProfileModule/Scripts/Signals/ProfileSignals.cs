using FlowIoC.BaseModule.Signals;
using Modules.ProfileModule.Shared.Data.ValueObjects;

namespace Modules.ProfileModule.Signals
{
    public class ProfileSignals : ISignalHolder
    {
        public ProfileSignalsIncoming Incoming = new();
        public ProfileSignalsOutgoing Outgoing = new();
    }

    public class ProfileSignalsIncoming
    {
        public Signal RequestCurrentIcon = new();
        public Signal OpenSelection = new();
        public Signal<ProfileEditVO> SaveProfile = new();
    }

    public class ProfileSignalsOutgoing
    {
        public Signal<ProfileIconVO> IconChanged = new();
        public Signal<ProfileFrameVO> FrameChanged = new();
        public Signal<ProfileSelectionRVO> SelectionRequested = new();
        public Signal<ProfileSelectionRVO> ProfileSaved = new();
    }
}
