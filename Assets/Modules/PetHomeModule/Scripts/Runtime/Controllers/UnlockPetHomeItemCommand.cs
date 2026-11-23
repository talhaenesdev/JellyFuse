using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PetHomeModule.Models;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using Modules.PetHomeModule.Signals;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>
    /// The purse answered: paid, the item is the player's and goes into the room; not paid, the screen is told and the
    /// flow stops. An answer to somebody else's key is not this module's and stops the flow too.
    /// </summary>
    internal sealed class UnlockPetHomeItemCommand : Command
    {
        [Inject] private IPetHomeModel _model { get; set; }
        [InjectSignal] private PetHomeSignals _signals { get; set; }
        [SignalParam] private string _key { get; set; }
        [SignalParam] private bool _paid { get; set; }

        public override void Execute()
        {
            if (string.IsNullOrEmpty(_key) || !_key.StartsWith(PickPetHomeItemCommand.KEY_PREFIX)) { Retain(); Stop(); return; }
            string id = _key.Substring(PickPetHomeItemCommand.KEY_PREFIX.Length);
            if (!_paid)
            {
                _signals.Outgoing.NotBought.Dispatch(id);
                Retain();
                Stop();
                return;
            }
            _model.Own(id);
            _model.Use(id);
        }
    }
}
