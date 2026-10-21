using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.SettingsScreenModule.Models
{
    internal interface ISettingsModel
    {
        bool IsEnabled(SettingsOption option);
        bool Toggle(SettingsOption option);
    }
}
