using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Gives today's login reward and moves the run on a day. Taken already, or nothing to take, and the sequence stops.</summary>
    internal class ClaimLoginRewardCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var config = _shared.GetScriptable<CD_Events>();
            if (!EventsFunction.LoginClaimable(progress, config))
            {
                Retain();
                Stop();
                return;
            }
            EventsFunction.Grant(progress, config.LoginRewards[progress.LoginRewardsTaken]);
            progress.LoginRewardsTaken++;
            progress.LoginRewardDay = ShopDayFunction.Today();
            _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
        }
    }
}
