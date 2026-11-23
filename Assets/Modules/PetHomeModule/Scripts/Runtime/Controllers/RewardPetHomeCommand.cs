using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.PetHomeModule.Models;
using Modules.PetHomeModule.Signals;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>A pet whose happiness just filled up earns the player coins: they go to the purse and the screen is told.</summary>
    internal sealed class RewardPetHomeCommand : Command
    {
        [Inject] private IPetHomeModel _model { get; set; }
        [InjectSignal] private PetHomeSignals _signals { get; set; }

        public override void Execute()
        {
            int coins = _model.TakeReward();
            if (coins <= 0) return;
            _signals.Outgoing.GrantCoins.Dispatch(coins);
            _signals.Outgoing.Rewarded.Dispatch(coins);
        }
    }
}
