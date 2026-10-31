using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// What a rewarded ad the player chose to watch gives: the win's coins again, more time on a level
    /// that ran out of it, a free booster once a day. Filed in GameplaySystemRoot's Shared Scriptables.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_AdRewards", menuName = "Jelly Fuse/Ad Rewards")]
    public sealed class CD_AdRewards : ScriptableObject
    {
        [Tooltip("The win's coins are multiplied by this for a watched ad; 2 doubles them. 1 or less offers no ad.")]
        [Min(1)] public int WinRewardMultiplier = 2;
        [Tooltip("Seconds a watched ad gives a level that ran out of time, once a level. 0 offers no ad.")]
        [Min(0)] public float ExtraSeconds = 30;
        [Tooltip("Uses of a booster a watched ad gives, once a day, on a booster with none left. 0 offers no ad.")]
        [Min(0)] public int DailyFreeBoosters = 1;

        [Header("Button texts")]
        [Tooltip("Localization key of the win panel's ad button; {0} is the multiplier.")]
        public string DoubleRewardKey = "gameplay.ad_double_reward";
        [Tooltip("Localization key of the lose panel's ad button; {0} is the seconds.")]
        public string ExtraTimeKey = "gameplay.ad_extra_time";
    }
}
