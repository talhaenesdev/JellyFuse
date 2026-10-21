#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Signals;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.GameplayScreenModule.GameplayScreenTestModule.Controllers;
using Modules.GameplayModule.GameplayScreenModule.Signals;

namespace Modules.GameplayModule.GameplayScreenModule.GameplayScreenTestModule.RootsContexts
{
    [ExcludeFromContextWindow]
    public class GameplayScreenTestContext : BaseScreenContext
    {
        private readonly Signal _verify = new();
        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_verify).ToSequence<VerifyJellyRulesCommand>();
        }
        public override void Launch()
        {
            base.Launch();
            _verify.Dispatch();
            InjectionBinderCrossContext.GetInstance<GameplayScreenSignals>().Incoming.OpenGameplayScreen.Dispatch();
        }
    }
}
#endif
