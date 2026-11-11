using System;

namespace Modules.AdsModule.Services
{
    /// <summary>Rewarded ads through Unity LevelPlay. Answers the caller that asked.</summary>
    public partial interface IAdsService
    {
        /// <summary>
        /// Starts LevelPlay with the app filed in CD_Ads and keeps a rewarded ad loaded from then on.
        /// <paramref name="consent"/> is the player's answer to the consent question: false keeps the ads
        /// non-personalised. Called again once started, it only passes on a changed answer.
        /// </summary>
        void Start(bool consent);

        /// <summary>A rewarded ad is loaded and can be shown now.</summary>
        bool IsRewardedReady { get; }

        /// <summary>Shows the rewarded ad; <paramref name="onDone"/> hears true once it closes having earned its reward.</summary>
        void ShowRewarded(Action<bool> onDone);

        /// <summary>The steps this Service ships for a sequence to wait on.</summary>
        public static partial class Commands { }
    }
}
