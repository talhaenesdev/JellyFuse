using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.StoreModule.Services;
using Modules.StoreModule.Signals;

namespace Modules.StoreModule.Controllers
{
    /// <summary>Announces the prices the store already answered with; before it has, there is nothing to say.</summary>
    internal class AnnouncePricesCommand : Command
    {
        [Inject] private IStoreService _store { get; set; }
        [InjectSignal] private StoreSignals _signals { get; set; }

        public override void Execute()
        {
            if (_store.Prices != null) _signals.Outgoing.PricesChanged.Dispatch(_store.Prices);
        }
    }
}
