#if UNITY_EDITOR
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Signals;
using Modules.HapticModule.HapticTestModule.Controllers;
namespace Modules.HapticModule.HapticTestModule.RootsContexts
{
    public sealed class HapticTestContext : Context
    {
        private readonly Signal _run = new();
        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_run).ToSequence<RunHapticChecksCommand>();
        }
        public override void Launch() { base.Launch(); _run.Dispatch(); }
    }
}
#endif
