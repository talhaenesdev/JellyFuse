using Modules.ProfileModule.Shared.Data.ValueObjects;

namespace Modules.ProfileModule.Models
{
    public interface IProfileModel
    {
        ProfileIconVO CurrentIcon { get; }
        ProfileFrameVO CurrentFrame { get; }
        ProfileSelectionRVO CreateSelection();
        bool Save(ProfileEditVO edit);

        /// <summary>Reads the selected icon and frame from SD_PlayerProfile again, after its values were replaced.</summary>
        void Reload();
    }
}
