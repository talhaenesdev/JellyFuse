using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.StoreModule.Models;
using Modules.StoreModule.Services;
using Modules.StoreModule.Signals;

namespace Modules.StoreModule.Controllers
{
    /// <summary>Connects to the store with the catalog's products; prices and payments come back as signals.</summary>
    internal class ConnectStoreCommand : Command
    {
        [Inject] private StoreModel _model { get; set; }
        [Inject] private IStoreService _store { get; set; }
        [InjectSignal] private StoreSignals _signals { get; set; }

        public override void Execute()
        {
            if (_model.Catalog == null) return;
            var outgoing = _signals.Outgoing;
            _store.Connect(_model.Catalog.Products, outgoing.PricesChanged.Dispatch, outgoing.Purchased.Dispatch);
        }
    }
}
