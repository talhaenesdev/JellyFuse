using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>A booster just used counts toward today's booster quests.</summary>
    internal class CountBoosterUseCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (progress == null) return;
            EventsFunction.Advance(progress, QuestType.UseBooster, 1);
            progress.Stats ??= new();
            progress.Stats.BoostersUsed++;
        }
    }
}
