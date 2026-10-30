using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.StoreModule.Services;

namespace Modules.StoreModule.Controllers
{
    internal class BuyProductCommand : Command
    {
        [Inject] private IStoreService _store { get; set; }
        [SignalParam] private string _productId { get; set; }

        public override void Execute()
        {
            if (!_store.Buy(_productId)) FlowLogger.LogWarning($"Store cannot sell {_productId} yet");
        }
    }
}
