using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Shared.Constants;
using UnityEngine;

namespace Modules.GameplayModule.SettingsScreenModule.Models
{
    internal sealed class SettingsModel : ISettingsModel
    {
        public bool IsEnabled(SettingsOption option) => PlayerPrefs.GetInt(SettingsConstants.PREFS_PREFIX + option, 1) == 1;

        public bool Toggle(SettingsOption option)
        {
            bool enabled = !IsEnabled(option);
            PlayerPrefs.SetInt(SettingsConstants.PREFS_PREFIX + option, enabled ? 1 : 0);
            PlayerPrefs.Save();
            return enabled;
        }
    }
}
