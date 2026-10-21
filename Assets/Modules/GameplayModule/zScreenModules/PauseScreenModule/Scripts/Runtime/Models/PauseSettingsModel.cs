using Modules.GameplayModule.Shared.Constants;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.PauseScreenModule.Models
{
    /// <summary>The same PlayerPrefs entries the settings screen keeps, so both show one value.</summary>
    internal sealed class PauseSettingsModel : IPauseSettingsModel
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
