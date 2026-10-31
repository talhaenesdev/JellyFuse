using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// The ad was watched: the level lost to the clock is played on with CD_AdRewards' extra seconds, and the life
    /// the time-out took is given back, since the level was not lost after all.
    /// </summary>
    internal class ExtendTimeCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private ILivesModel _lives { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            _model.ExtendTime(_shared.GetScriptable<CD_AdRewards>().ExtraSeconds);
            var lives = _shared.GetScriptable<CD_Lives>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (_model.LifeTaken && lives != null && progress != null)
            {
                _model.LifeTaken = false;
                _lives.Add(progress, lives, 1, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }
            _signals.Outgoing.BoardChanged.Dispatch(new PuzzleTurnVO(new[] { _model.Snapshot() }));
            _signals.Outgoing.TimeChanged.Dispatch(_model.RemainingSeconds);
        }
    }
}
