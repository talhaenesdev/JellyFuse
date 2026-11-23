using FlowIoC.BaseModule.Signals;
using Modules.PetHomeModule.Shared.Data.ValueObjects;

namespace Modules.PetHomeModule.PetHomeScreenModule.Signals
{
    public class PetHomeScreenSignals : ISignalHolder
    {
        public PetHomeScreenSignalsIncoming Incoming = new();
        public PetHomeScreenSignalsOutgoing Outgoing = new();
    }

    public class PetHomeScreenSignalsIncoming
    {
        /// <summary>Open the pet home as it stands.</summary>
        public Signal<PetHomeRVO> Open = new();
        /// <summary>The pet home changed - bought, stored, a new room.</summary>
        public Signal<PetHomeRVO> Apply = new();
        /// <summary>The coins the player holds now.</summary>
        public Signal<int> ApplyCoins = new();
        /// <summary>An item could not be bought, by id.</summary>
        public Signal<string> NotBought = new();
    }

    public class PetHomeScreenSignalsOutgoing
    {
        /// <summary>A shop card was pressed, by id.</summary>
        public Signal<string> PickClicked = new();
        /// <summary>An item was dragged to a new spot.</summary>
        public Signal<PlacedItemSVO> Placed = new();
        /// <summary>An item was taken out of the room, by id.</summary>
        public Signal<string> Stored = new();
        /// <summary>The pet ate a bowl empty, by id.</summary>
        public Signal<string> Ate = new();
        /// <summary>An empty bowl was tapped to fill it, by id.</summary>
        public Signal<string> RefillClicked = new();
        /// <summary>The screen was closed.</summary>
        public Signal Closed = new();
    }
}
