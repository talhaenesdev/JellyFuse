using System.Collections.Generic;
using FlowIoC.BaseModule.Signals;
using Modules.PurchaseModule.Shared.Data.ValueObjects;

namespace Modules.PurchaseModule.PurchaseScreenModule.Signals
{
    public class PurchaseScreenSignals : ISignalHolder
    {
        public PurchaseScreenSignalsIncoming Incoming = new();
        public PurchaseScreenSignalsOutgoing Outgoing = new();
    }

    public class PurchaseScreenSignalsIncoming
    {
        /// <summary>Opens the popup on this offer.</summary>
        public Signal<PurchaseOfferVO> Open = new();
        /// <summary>The offer changed - bought, refilled: the popup redraws it while it shows that offer.</summary>
        public Signal<PurchaseOfferVO> ApplyOffer = new();
    }

    public class PurchaseScreenSignalsOutgoing
    {
        /// <summary>The popup closed itself.</summary>
        public Signal Closed = new();

        private readonly Dictionary<string, Signal> _bought = new();
        private readonly Dictionary<string, Signal> _refresh = new();

        /// <summary>Buy was pressed on the offer with this key. One signal per key, so a Connector wires each seller without an if.</summary>
        public Signal Bought(string key)
        {
            if (!_bought.TryGetValue(key, out var signal)) _bought[key] = signal = new Signal();
            return signal;
        }
        /// <summary>The countdown of the offer with this key ran out: the seller should send the offer again.</summary>
        public Signal RefreshRequested(string key)
        {
            if (!_refresh.TryGetValue(key, out var signal)) _refresh[key] = signal = new Signal();
            return signal;
        }
    }
}
