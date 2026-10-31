using UnityEngine;

namespace Modules.AdsModule.Data.UnityObjects
{
    /// <summary>The LevelPlay app and ad units. The defaults are LevelPlay's own test app, which only shows test ads.</summary>
    [CreateAssetMenu(fileName = "CD_Ads", menuName = "Ads/CD_Ads")]
    public sealed class CD_Ads : ScriptableObject
    {
        [Tooltip("The Android app key from the LevelPlay dashboard.")]
        public string AndroidAppKey = "85460dcd";
        [Tooltip("The Android rewarded ad unit id from the LevelPlay dashboard.")]
        public string AndroidRewardedAdUnitId = "76yy3nay3ceui2a3";
    }
}
