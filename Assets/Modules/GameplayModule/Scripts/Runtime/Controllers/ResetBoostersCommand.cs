using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Forgets the player's boosters: no uses left and no free uses given, as for a new player. The
    /// step after it gives the free uses again. With no progress asset filed there is nothing to
    /// reset, and the sequence stops here.
    /// </summary>
    internal class ResetBoostersCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (progress == null)
            {
                Retain();
                Stop();
                return;
            }
            progress.Boosters.Clear();
        }
    }
}
