using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.PetHomeModule.PetHomeScreenModule.Controllers;
using Modules.PetHomeModule.PetHomeScreenModule.Signals;
using Modules.PetHomeModule.PetHomeScreenModule.ViewsMediators;

namespace Modules.PetHomeModule.PetHomeScreenModule.RootsContexts
{
    public class PetHomeScreenContext : ScreenSubContext<PetHomeScreenView, PetHomeScreenMediator>
    {
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 1,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("PetHomeScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private PetHomeScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            InjectionBinder.Bind<PetHomeScreenInternalSignals>();
            _signals = InjectionBinderCrossContext.Bind<PetHomeScreenSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.Open)
                .ToSequence<OpenPetHomeScreenCommand>()
                .ToSequence<LoadPetHomeAnimalCommand>();
        }
    }
}
