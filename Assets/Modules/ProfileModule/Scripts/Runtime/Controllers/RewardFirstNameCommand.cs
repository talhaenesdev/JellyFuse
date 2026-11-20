using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.ProfileModule.Models;
using Modules.ProfileModule.Signals;

namespace Modules.ProfileModule.Controllers
{
    /// <summary>
    /// The profile was saved: the first time it carries a name of the player's own rather than the default, the
    /// name reward is paid - once, kept in SD_PlayerProfile, which the next step writes.
    /// </summary>
    internal class RewardFirstNameCommand : Command
    {
        [Inject] private IProfileModel _model { get; set; }
        [InjectSignal] private ProfileSignals _signals { get; set; }

        public override void Execute()
        {
            if (!_model.ClaimNameReward()) return;
            FlowLogger.Log($"RewardFirstNameCommand - first name picked, {ProfileModel.NAME_REWARD_COINS} coins.");
            _signals.Outgoing.NameRewardEarned.Dispatch(ProfileModel.NAME_REWARD_COINS);
        }
    }
}
