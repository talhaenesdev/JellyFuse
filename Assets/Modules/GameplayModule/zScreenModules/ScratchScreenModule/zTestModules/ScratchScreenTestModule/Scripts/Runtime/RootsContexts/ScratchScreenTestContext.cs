#if UNITY_EDITOR
using Modules.GameplayModule.ScratchScreenModule.ViewsMediators;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.ScreenModule.RootsContexts;

namespace Modules.GameplayModule.ScratchScreenModule.ScratchScreenTestModule.RootsContexts
{
    /// <summary>
    /// A screen's test context opens the screen and nothing else: the production context, listed
    /// as a sub-context on the test Root, brings the signals, the mediation and the ScreenCVO.
    /// </summary>
    [ExcludeFromContextWindow]
    public class ScratchScreenTestContext : BaseScreenContext
    {
        public override void Launch()
        {
            base.Launch();
			_screenService.Open<ScratchScreenView>().Show();

        }
    }
}
#endif
