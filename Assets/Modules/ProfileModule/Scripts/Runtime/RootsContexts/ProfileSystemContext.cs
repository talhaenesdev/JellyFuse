using Modules.ProfileModule.Signals;
using FlowIoC.BaseModule.Contexts;
using Modules.LocalSaveModule.Services;
using Modules.ProfileModule.Controllers;
using Modules.ProfileModule.Models;

namespace Modules.ProfileModule.RootsContexts
{

    public class ProfileSystemContext : Context
    {
		private ProfileSignals _signals;

		private ProfileInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
			_internalSignals = InjectionBinder.Bind<ProfileInternalSignals>();
			_signals = InjectionBinderCrossContext.Bind<ProfileSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<IProfileModel, ProfileModel>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.RequestCurrentIcon).ToSequence<PublishCurrentProfileIconCommand>();
            CommandBinder.Bind(_signals.Incoming.ReloadProfile)
                .ToSequence<ReloadProfileCommand>()
                .ToSequence<PublishCurrentProfileIconCommand>();
            CommandBinder.Bind(_signals.Incoming.OpenSelection).ToSequence<OpenProfileSelectionCommand>();
            CommandBinder.Bind(_signals.Incoming.SaveProfile)
                .ToSequence<SaveProfileCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>("SD_PlayerProfile");
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
