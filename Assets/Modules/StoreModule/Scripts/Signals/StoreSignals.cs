using System.Collections.Generic;
using FlowIoC.BaseModule.Signals;

namespace Modules.StoreModule.Signals
{
    public class StoreSignals : ISignalHolder
    {
        public StoreSignalsIncoming Incoming = new();
        public StoreSignalsOutgoing Outgoing = new();
    }

    public class StoreSignalsIncoming
    {
        /// <summary>Buy the product with this id.</summary>
        public Signal<string> Buy = new();
        /// <summary>Announce the prices again, for a shop that opened after they came.</summary>
        public Signal RequestPrices = new();
    }

    public class StoreSignalsOutgoing
    {
        /// <summary>The store's localized price of every product it sells, by product id.</summary>
        public Signal<IReadOnlyDictionary<string, string>> PricesChanged = new();
        /// <summary>A product was paid for, or an owned non-consumable was found at launch. Grant it now.</summary>
        public Signal<string> Purchased = new();
    }
}
