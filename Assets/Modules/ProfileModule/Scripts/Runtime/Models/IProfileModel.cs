using Modules.ProfileModule.Shared.Data.ValueObjects;

namespace Modules.ProfileModule.Models
{
    public interface IProfileModel
    {
        ProfileIconVO CurrentIcon { get; }
        ProfileFrameVO CurrentFrame { get; }
        ProfileSelectionRVO CreateSelection();
        /// <summary>Marks the name reward paid when the saved name earns it; false when it does not.</summary>
        bool ClaimNameReward();
        bool NameRewardAvailable { get; }
        bool Save(ProfileEditVO edit);

        /// <summary>Reads the selected icon and frame from SD_PlayerProfile again, after its values were replaced.</summary>
        void Reload();
        /// <summary>The coins an icon costs to unlock; 0 when free or already the player's, -1 when there is no such icon.</summary>
        int PriceToUnlock(string iconId);
        /// <summary>The icon was paid for: it is the player's from now on.</summary>
        void Unlock(string iconId);
    }
}
