using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PetHomeModule.Models;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using Modules.PetHomeModule.Signals;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>
    /// A shop card was pressed. Owned or free, it goes into the room (or the room is moved into) and the flow carries on
    /// to the save; otherwise its price is asked of the purse and this flow ends - the purse's answer starts the next.
    /// </summary>
    internal sealed class PickPetHomeItemCommand : Command
    {
        public const string KEY_PREFIX = "pethome:";

        [Inject] private IPetHomeModel _model { get; set; }
        [InjectSignal] private PetHomeSignals _signals { get; set; }
        [SignalParam] private string _id { get; set; }

        public override void Execute()
        {
            var item = _model.Item(_id);
            if (item == null) { Retain(); Stop(); return; }
            if (_model.Owns(_id) || item.Price <= 0)
            {
                _model.Own(_id);
                _model.Use(_id);
                return;
            }
            _signals.Outgoing.CoinsRequested.Dispatch(KEY_PREFIX + _id, item.Price);
            Retain();
            Stop();
        }
    }
}
