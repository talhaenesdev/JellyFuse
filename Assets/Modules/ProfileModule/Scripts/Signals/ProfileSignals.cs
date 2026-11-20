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
        /// <summary>SD_PlayerProfile was replaced (a restore from the cloud): read it again and announce the icon.</summary>
        public Signal ReloadProfile = new();
        public Signal OpenSelection = new();
        public Signal<ProfileEditVO> SaveProfile = new();
        /// <summary>The player asked to buy a locked icon, by id.</summary>
        public Signal<string> BuyIcon = new();
        /// <summary>The purse answered a CoinsRequested: the key it was asked with, and whether the coins were taken.</summary>
        public Signal<string, bool> CoinsAnswered = new();
    }

    public class ProfileSignalsOutgoing
    {
        public Signal<ProfileIconVO> IconChanged = new();
        public Signal<ProfileFrameVO> FrameChanged = new();
        public Signal<ProfileSelectionRVO> SelectionRequested = new();
        public Signal<ProfileSelectionRVO> ProfileSaved = new();
        /// <summary>The saved name and icon were sent on to the leaderboard (or could not be, offline).</summary>
        public Signal ProfilePublished = new();
        /// <summary>The first name the player picked earned these coins.</summary>
        public Signal<int> NameRewardEarned = new();
        /// <summary>Whether the first-name reward still waits to be claimed - the profile button carries a dot while it does.</summary>
        public Signal<bool> NameRewardPending = new();
        /// <summary>An icon is to be paid for: a key to answer with ("icon:" and its id) and the coins it costs.</summary>
        public Signal<string, int> CoinsRequested = new();
        /// <summary>An icon was bought: the selection again, with it unlocked.</summary>
        public Signal<ProfileSelectionRVO> IconBought = new();
        /// <summary>An icon could not be bought - not enough coins - by id.</summary>
        public Signal<string> IconNotBought = new();
    }
}
