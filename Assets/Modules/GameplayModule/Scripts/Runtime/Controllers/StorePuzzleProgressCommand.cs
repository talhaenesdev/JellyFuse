using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Writes the level being played into SD_JellyProgress, so the next run starts there. The step
    /// after it writes the asset to disk. With no progress asset filed (a test scene without
    /// LocalSaveServiceRoot) there is nothing to keep, and the sequence stops here.
    /// </summary>
    internal class StorePuzzleProgressCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (progress == null)
            {
                Retain();
                Stop();
                return;
            }
            progress.LevelIndex = _model.LevelIndex;
        }
    }
}
