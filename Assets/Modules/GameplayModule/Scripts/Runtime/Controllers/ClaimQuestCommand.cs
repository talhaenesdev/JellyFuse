using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Gives a finished quest's reward, once. A quest not finished, or taken already, stops the sequence.</summary>
    internal class ClaimQuestCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private int _index { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var quests = progress != null ? progress.Quests : null;
            if (quests == null || _index < 0 || _index >= quests.Count || !quests[_index].IsClaimable)
            {
                Retain();
                Stop();
                return;
            }
            quests[_index].Claimed = true;
            EventsFunction.Grant(progress, quests[_index].Reward);
            _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
        }
    }
}
