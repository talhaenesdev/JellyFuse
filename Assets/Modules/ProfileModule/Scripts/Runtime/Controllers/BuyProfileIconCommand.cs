using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.ProfileModule.Models;
using Modules.ProfileModule.Signals;

namespace Modules.ProfileModule.Controllers
{
    /// <summary>A locked icon was asked for: its price is asked of the purse. One already owned is handed back as bought.</summary>
    internal sealed class BuyProfileIconCommand : Command
    {
        public const string KEY_PREFIX = "icon:";

        [Inject] private IProfileModel _model { get; set; }
        [InjectSignal] private ProfileSignals _signals { get; set; }
        [SignalParam] private string _iconId { get; set; }

        public override void Execute()
        {
            int price = _model.PriceToUnlock(_iconId);
            if (price < 0) return;
            if (price == 0) { _signals.Outgoing.IconBought.Dispatch(_model.CreateSelection()); return; }
            _signals.Outgoing.CoinsRequested.Dispatch(KEY_PREFIX + _iconId, price);
        }
    }
}
