using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.PauseScreenModule.Models
{
    internal interface IPauseSettingsModel
    {
        bool IsEnabled(SettingsOption option);
        bool Toggle(SettingsOption option);
    }
}
