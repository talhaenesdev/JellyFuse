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
        /// <summary>An icon was bought: the same choices, with it unlocked; the screen stays open.</summary>
        public Signal<ProfileSelectionRVO> IconBought = new();
        /// <summary>An icon could not be bought - not enough coins.</summary>
        public Signal<string> IconNotBought = new();
    }

    public class ProfileSelectionScreenSignalsOutgoing
    {
        public Signal<ProfileEditVO> SaveRequested = new();
        /// <summary>The stats button was pressed: the player's stats popup opens over the screen.</summary>
        public Signal StatsClicked = new();
        /// <summary>Buy was pressed on a locked icon, by id.</summary>
        public Signal<string> BuyRequested = new();
    }
}
