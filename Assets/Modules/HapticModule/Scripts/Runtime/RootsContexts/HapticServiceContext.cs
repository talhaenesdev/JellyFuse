using FlowIoC.BaseModule.Contexts;
using Modules.HapticModule.Controllers;
using Modules.HapticModule.Models;
using Modules.HapticModule.Services;
using Modules.HapticModule.Signals;
namespace Modules.HapticModule.RootsContexts
{
    public sealed class HapticServiceContext : Context
    {
        private HapticInternalSignals _signals;
        private IHapticDevice _device;
        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinder.Bind<HapticInternalSignals>();
        }
        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<HapticModel>();
            _device = InjectionBinder.Bind<IHapticDevice, HapticDevice>();
            InjectionBinderCrossContext.Bind<IHapticService, HapticService>();
        }
        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Play).ToSequence<PlayHapticCommand>();
            CommandBinder.Bind(_signals.PlayFor).ToSequence<PlayHapticForCommand>();
            CommandBinder.Bind(_signals.SetEnabled).ToSequence<SetHapticEnabledCommand>();
        }
        public override void DestroyContext()
        {
            _device?.Dispose();
            base.DestroyContext();
        }
    }
}
