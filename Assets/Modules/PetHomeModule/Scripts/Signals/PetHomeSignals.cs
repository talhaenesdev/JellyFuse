using FlowIoC.BaseModule.Signals;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using Modules.PetHomeModule.Shared.Enums;

namespace Modules.PetHomeModule.Signals
{
    public class PetHomeSignals : ISignalHolder
    {
        public PetHomeSignalsIncoming Incoming = new();
        public PetHomeSignalsOutgoing Outgoing = new();
    }

    public class PetHomeSignalsIncoming
    {
        /// <summary>The player asked for the pet home.</summary>
        public Signal Open = new();
        /// <summary>A shop card was pressed, by id: buy it, put it in the room, or move into it if it is a room.</summary>
        public Signal<string> Pick = new();
        /// <summary>An item was dragged to a new spot.</summary>
        public Signal<PlacedItemSVO> Place = new();
        /// <summary>An item was taken out of the room, by id; it stays the player's.</summary>
        public Signal<string> Store = new();
        /// <summary>The pet ate a bowl empty, by id.</summary>
        public Signal<string> Eat = new();
        /// <summary>An empty bowl was filled again, by id.</summary>
        public Signal<string> Refill = new();
        /// <summary>Something was done for the pet: it is happier.</summary>
        public Signal<PetHomeCare> Care = new();
        /// <summary>The purse answered a CoinsRequested: the key it was asked with, and whether the coins were taken.</summary>
        public Signal<string, bool> CoinsAnswered = new();
    }

    public class PetHomeSignalsOutgoing
    {
        /// <summary>The pet home is to be shown, as it stands.</summary>
        public Signal<PetHomeRVO> Opened = new();
        /// <summary>Something changed - bought, moved, stored, a new room.</summary>
        public Signal<PetHomeRVO> Changed = new();
        /// <summary>An item is to be paid for: a key to answer with ("pethome:" and its id) and the coins it costs.</summary>
        public Signal<string, int> CoinsRequested = new();
        /// <summary>An item could not be bought - not enough coins - by id.</summary>
        public Signal<string> NotBought = new();
        /// <summary>The pet's happiness filled up: these coins are the player's.</summary>
        public Signal<int> Rewarded = new();
        /// <summary>Coins to add to the player's purse.</summary>
        public Signal<int> GrantCoins = new();
    }
}
