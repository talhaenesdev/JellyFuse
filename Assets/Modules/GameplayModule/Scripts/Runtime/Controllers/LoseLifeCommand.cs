using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>A failed level costs a life, once, however it was lost.</summary>
    internal class LoseLifeCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILivesModel _lives { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_Lives>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (config == null || progress == null || _model.Status != Shared.Enums.PuzzleStatus.Lost || _model.LifeTaken) return;
            _model.LifeTaken = true;
            _lives.Lose(progress, config, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
    }
}
