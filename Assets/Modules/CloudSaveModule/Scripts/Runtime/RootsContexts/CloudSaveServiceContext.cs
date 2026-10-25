using FlowIoC.BaseModule.Contexts;
using Modules.CloudSaveModule.Services;
using Modules.CloudSaveModule.Signals;

namespace Modules.CloudSaveModule.RootsContexts
{
    public class CloudSaveServiceContext : Context
    {
        private CloudSaveInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _internalSignals = InjectionBinder.Bind<CloudSaveInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinderCrossContext.Bind<ICloudSaveService, CloudSaveService>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
        }

        public override void Setup()
        {
            base.Setup();
        }

        public override void Launch()
        {
            base.Launch();
        }
    }
}
