using FlowIoC.BaseModule.Contexts;
using Modules.LocalSaveModule.Services;
using Modules.PetHomeModule.Controllers;
using Modules.PetHomeModule.Models;
using Modules.PetHomeModule.Signals;

namespace Modules.PetHomeModule.RootsContexts
{
    public class PetHomeSystemContext : Context
    {
        private const string SAVE = "SD_PetHome";

        private PetHomeSignals _signals;
        private PetHomeInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _internalSignals = InjectionBinder.Bind<PetHomeInternalSignals>();
            _signals = InjectionBinderCrossContext.Bind<PetHomeSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<IPetHomeModel, PetHomeModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.Open)
                .ToSequence<GivePetHomeStartersCommand>()
                .ToSequence<PassPetHomeTimeCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(SAVE)
                .ToSequence<OpenPetHomeCommand>();
            CommandBinder.Bind(_signals.Incoming.Pick)
                .ToSequence<PickPetHomeItemCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(SAVE)
                .ToSequence<AnnouncePetHomeCommand>();
            CommandBinder.Bind(_signals.Incoming.CoinsAnswered)
                .ToSequence<UnlockPetHomeItemCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(SAVE)
                .ToSequence<AnnouncePetHomeCommand>();
            CommandBinder.Bind(_signals.Incoming.Place)
                .ToSequence<PlacePetHomeItemCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(SAVE);
            CommandBinder.Bind(_signals.Incoming.Eat)
                .ToSequence<EmptyPetHomeBowlCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(SAVE)
                .ToSequence<AnnouncePetHomeCommand>();
            CommandBinder.Bind(_signals.Incoming.Refill)
                .ToSequence<RefillPetHomeBowlCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(SAVE)
                .ToSequence<AnnouncePetHomeCommand>();
            CommandBinder.Bind(_signals.Incoming.Care)
                .ToSequence<CarePetHomeCommand>()
                .ToSequence<RewardPetHomeCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(SAVE)
                .ToSequence<AnnouncePetHomeCommand>();
            CommandBinder.Bind(_signals.Incoming.Store)
                .ToSequence<StorePetHomeItemCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(SAVE)
                .ToSequence<AnnouncePetHomeCommand>();
        }
    }
}
