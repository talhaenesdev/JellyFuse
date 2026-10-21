using FlowIoC.BaseModule.Signals;
using Modules.ProfileModule.Shared.Data.ValueObjects;

namespace Modules.ProfileModule.ProfileSelectionScreenModule.Signals
{
    public class ProfileSelectionScreenSignals : ISignalHolder
    {
        public ProfileSelectionScreenSignalsIncoming Incoming = new();
        public ProfileSelectionScreenSignalsOutgoing Outgoing = new();
    }

    public class ProfileSelectionScreenSignalsIncoming
    {
        public Signal<ProfileSelectionRVO> Open = new();
        public Signal<ProfileSelectionRVO> ProfileSaved = new();
    }

    public class ProfileSelectionScreenSignalsOutgoing
    {
        public Signal<ProfileEditVO> SaveRequested = new();
    }
}
