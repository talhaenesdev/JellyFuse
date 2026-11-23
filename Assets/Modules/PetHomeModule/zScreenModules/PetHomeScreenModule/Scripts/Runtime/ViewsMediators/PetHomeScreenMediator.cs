using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.PetHomeModule.PetHomeScreenModule.Signals;
using Modules.PetHomeModule.Shared.Data.ValueObjects;

namespace Modules.PetHomeModule.PetHomeScreenModule.ViewsMediators
{
    public class PetHomeScreenMediator : IMediator
    {
        [Inject] private PetHomeScreenView _view { get; set; }
        [InjectSignal] private PetHomeScreenSignals _signals { get; set; }
        [InjectSignal] private PetHomeScreenInternalSignals _internalSignals { get; set; }

        public virtual void OnRegister()
        {
            _view.ShowCompleted += OnScreenShown;
            _view.HideCompleted += OnScreenHidden;
        }

        public virtual void OnRemove()
        {
            _view.ShowCompleted -= OnScreenShown;
            _view.HideCompleted -= OnScreenHidden;
        }

        private void OnScreenShown(IScreenBody screen)
        {
            _view.CloseClicked += OnClose;
            _view.PickClicked += OnPick;
            _view.Placed += OnPlaced;
            _view.Stored += OnStored;
            _view.Ate += OnAte;
            _view.RefillClicked += OnRefill;
            _signals.Incoming.Apply.AddListener(_view.Apply);
            _signals.Incoming.ApplyCoins.AddListener(_view.ApplyCoins);
            _signals.Incoming.NotBought.AddListener(_view.ShowNotBought);
            _internalSignals.AnimalLoaded.AddListener(_view.ApplyAnimal);
        }

        private void OnScreenHidden(IScreenBody screen)
        {
            _view.CloseClicked -= OnClose;
            _view.PickClicked -= OnPick;
            _view.Placed -= OnPlaced;
            _view.Stored -= OnStored;
            _view.Ate -= OnAte;
            _view.RefillClicked -= OnRefill;
            _signals.Incoming.Apply.RemoveListener(_view.Apply);
            _signals.Incoming.ApplyCoins.RemoveListener(_view.ApplyCoins);
            _signals.Incoming.NotBought.RemoveListener(_view.ShowNotBought);
            _internalSignals.AnimalLoaded.RemoveListener(_view.ApplyAnimal);
        }

        private bool Ready => _view.Data.HasState(ScreenState.AvailableToSendSignal);

        private void OnClose()
        {
            if (!Ready) return;
            _signals.Outgoing.Closed.Dispatch();
            _view.Hide();
        }

        private void OnPick(string id) { if (Ready) _signals.Outgoing.PickClicked.Dispatch(id); }
        private void OnPlaced(PlacedItemSVO placed) { if (Ready) _signals.Outgoing.Placed.Dispatch(placed); }
        private void OnStored(string id) { if (Ready) _signals.Outgoing.Stored.Dispatch(id); }
        private void OnAte(string id) { if (Ready) _signals.Outgoing.Ate.Dispatch(id); }
        private void OnRefill(string id) { if (Ready) _signals.Outgoing.RefillClicked.Dispatch(id); }
    }
}
