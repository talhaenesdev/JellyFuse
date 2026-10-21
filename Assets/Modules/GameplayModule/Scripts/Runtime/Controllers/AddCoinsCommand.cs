using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Adds the coins to the player's purse in SD_JellyProgress and announces what was earned and
    /// what is now held. The step after it writes the asset to disk. With no progress asset filed
    /// (a test scene without LocalSaveServiceRoot) there is no purse, and the sequence stops here.
    /// </summary>
    internal class AddCoinsCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private int _amount { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (progress == null)
            {
                Retain();
                Stop();
                return;
            }
            progress.Coins = Mathf.Max(0, progress.Coins + _amount);
            _signals.Outgoing.CoinsEarned.Dispatch(_amount);
            _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
        }
    }
}
