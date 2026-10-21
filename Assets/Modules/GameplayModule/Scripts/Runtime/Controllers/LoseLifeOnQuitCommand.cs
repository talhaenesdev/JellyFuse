using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Leaving a level that is still being played - back to the main menu - costs a life, as losing
    /// it would. A level already won or lost has settled its life, and is left for free.
    /// </summary>
    internal class LoseLifeOnQuitCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILivesModel _lives { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_Lives>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (config == null || progress == null || !_model.InPlay || _model.Status != PuzzleStatus.Playing || _model.LifeTaken) return;
            _model.LifeTaken = true;
            _lives.Lose(progress, config, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
    }
}
