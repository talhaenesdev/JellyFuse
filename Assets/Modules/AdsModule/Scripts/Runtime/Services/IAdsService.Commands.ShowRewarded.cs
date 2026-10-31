using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.AdsModule.Services
{
    public partial interface IAdsService
    {
        public static partial class Commands
        {
            /// <summary>Shows a rewarded ad. The sequence carries on once it was watched to the reward, and stops otherwise.</summary>
            [LongRetain]
            public class ShowRewarded : Command
            {
                [Inject] private IAdsService _ads { get; set; }

                public override void Execute()
                {
                    Retain();
                    if (!_ads.IsRewardedReady)
                    {
                        Stop();
                        return;
                    }
                    _ads.ShowRewarded(rewarded =>
                    {
                        if (rewarded) Release();
                        else Stop();
                    });
                }
            }
        }
    }
}
