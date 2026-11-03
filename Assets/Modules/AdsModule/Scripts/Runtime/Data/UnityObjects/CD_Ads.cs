using UnityEngine;

namespace Modules.AdsModule.Data.UnityObjects
{
    /// <summary>
    /// The LevelPlay app and ad units: the game's own, and LevelPlay's test app, which only shows test ads. Use Test
    /// Ads picks the test app for every player, so the ad flows work end to end while no real ad is shown.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_Ads", menuName = "Ads/CD_Ads")]
    public sealed class CD_Ads : ScriptableObject
    {
        [Tooltip("On: every player gets LevelPlay's test ads, and the game earns nothing from them. Off: the game's own ads.")]
        public bool UseTestAds = true;
        [Tooltip("The Android app key from the LevelPlay dashboard.")]
        public string AndroidAppKey = "85460dcd";
        [Tooltip("The Android rewarded ad unit id from the LevelPlay dashboard.")]
        public string AndroidRewardedAdUnitId = "76yy3nay3ceui2a3";

        [Header("LevelPlay's test app")]
        public string TestAndroidAppKey = "85460dcd";
        public string TestAndroidRewardedAdUnitId = "76yy3nay3ceui2a3";

        public string AppKey => UseTestAds ? TestAndroidAppKey : AndroidAppKey;
        public string RewardedAdUnitId => UseTestAds ? TestAndroidRewardedAdUnitId : AndroidRewardedAdUnitId;
    }
}
