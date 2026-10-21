#if UNITY_EDITOR
using FlowIoC.BaseModule.Controller;
using FlowIoC.ConsoleModule;
namespace Modules.HapticModule.HapticTestModule.Controllers
{
    public sealed class RunHapticChecksCommand : Command
    {
        public override void Execute() => FlowLogger.Log(new HapticChecks().Run());
    }
}
#endif
