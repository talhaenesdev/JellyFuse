using System;

namespace Modules.AdsModule.Services
{
    /// <summary>Rewarded ads through Unity LevelPlay. Answers the caller that asked.</summary>
    public partial interface IAdsService
    {
        /// <summary>Starts LevelPlay and keeps a rewarded ad loaded from then on.</summary>
        void Initialize(string appKey, string rewardedAdUnitId);

        /// <summary>A rewarded ad is loaded and can be shown now.</summary>
        bool IsRewardedReady { get; }

        /// <summary>Shows the rewarded ad; <paramref name="onDone"/> hears true once it closes having earned its reward.</summary>
        void ShowRewarded(Action<bool> onDone);

        /// <summary>The steps this Service ships for a sequence to wait on.</summary>
        public static partial class Commands { }
    }
}
