using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.ProfileModule.Models;
using Modules.ProfileModule.Signals;

namespace Modules.ProfileModule.Controllers
{
    /// <summary>
    /// The purse answered: paid, the icon becomes the player's and the step after it saves the profile; not paid, the
    /// screen is told and the flow stops. An answer to somebody else's key is not this module's and stops the flow too.
    /// </summary>
    internal sealed class UnlockProfileIconCommand : Command
    {
        [Inject] private IProfileModel _model { get; set; }
        [InjectSignal] private ProfileSignals _signals { get; set; }
        [SignalParam] private string _key { get; set; }
        [SignalParam] private bool _paid { get; set; }

        public override void Execute()
        {
            if (string.IsNullOrEmpty(_key) || !_key.StartsWith(BuyProfileIconCommand.KEY_PREFIX)) { Retain(); Stop(); return; }
            string iconId = _key.Substring(BuyProfileIconCommand.KEY_PREFIX.Length);
            if (!_paid)
            {
                _signals.Outgoing.IconNotBought.Dispatch(iconId);
                Retain();
                Stop();
                return;
            }
            _model.Unlock(iconId);
            _signals.Outgoing.IconBought.Dispatch(_model.CreateSelection());
        }
    }
}
