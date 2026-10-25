using FlowIoC.BaseModule.Contexts;
using Modules.CloudSyncModule.Controllers;
using Modules.CloudSyncModule.Signals;
using Modules.LocalSaveModule.Services;

namespace Modules.CloudSyncModule.RootsContexts
{
    public class CloudSyncSystemContext : Context
    {
        private CloudSyncSignals _signals;

        private CloudSyncInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _internalSignals = InjectionBinder.Bind<CloudSyncInternalSignals>();
            _signals = InjectionBinderCrossContext.Bind<CloudSyncSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            // A restore that took the cloud's copy is written to the local save at once; the upload
            // after it sends this device's copy whenever the cloud's did not win - a first launch,
            // or this device further ahead - so the cloud is current from the boot on.
            CommandBinder.Bind(_signals.Incoming.Restore)
                .ToSequence<RestoreCloudProgressCommand>()
                .ToSequence<ILocalSaveService.Commands.SaveAll>()
                .ToSequence<UploadCloudProgressCommand>();
            CommandBinder.Bind(_signals.Incoming.Upload).ToSequence<UploadCloudProgressCommand>();
        }

        public override void Setup()
        {
            base.Setup();
        }

        public override void Launch()
        {
            base.Launch();
        }

        /// <summary>
        /// Going to the background is when a mobile game is most often closed for good, so the
        /// progress goes to the cloud then too - the local save writes on the same pause.
        /// </summary>
        public override void PauseContext()
        {
            base.PauseContext();
            _signals?.Incoming.Upload.Dispatch();
        }
    }
}
