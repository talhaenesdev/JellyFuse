using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Another module asks for coins - a profile icon, say - under a key of its own: taken from the purse when it holds
    /// enough, and the answer goes back under the same key. Not enough, nothing is taken and the flow stops before the save.
    /// </summary>
    internal class SpendCoinsCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private string _key { get; set; }
        [SignalParam] private int _amount { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            bool paid = progress != null && _amount > 0 && progress.Coins >= _amount;
            if (paid)
            {
                progress.Coins -= _amount;
                _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
            }
            _signals.Outgoing.CoinsSpent.Dispatch(_key, paid);
            if (!paid) { Retain(); Stop(); }
        }
    }
}
