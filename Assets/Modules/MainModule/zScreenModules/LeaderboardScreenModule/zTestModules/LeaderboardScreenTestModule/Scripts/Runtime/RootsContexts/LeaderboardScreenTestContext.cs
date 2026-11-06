#if UNITY_EDITOR
using Modules.MainModule.LeaderboardScreenModule.ViewsMediators;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.ScreenModule.RootsContexts;

namespace Modules.MainModule.LeaderboardScreenModule.LeaderboardScreenTestModule.RootsContexts
{
    /// <summary>
    /// A screen's test context opens the screen and nothing else: the production context, listed
    /// as a sub-context on the test Root, brings the signals, the mediation and the ScreenCVO.
    /// </summary>
    [ExcludeFromContextWindow]
    public class LeaderboardScreenTestContext : BaseScreenContext
    {
        public override void Launch()
        {
            base.Launch();
			_screenService.Open<LeaderboardScreenView>().Show();

        }
    }
}
#endif
