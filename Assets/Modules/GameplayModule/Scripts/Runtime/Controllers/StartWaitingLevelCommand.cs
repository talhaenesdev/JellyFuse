using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Enums;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A life was bought while a level start waited for one: the offer closes and that start runs,
    /// so the player is back in the level rather than left on the panels. Nothing waits, nothing happens.
    /// </summary>
    internal class StartWaitingLevelCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILivesModel _lives { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var waiting = _lives.WaitingStart;
            if (waiting == WaitingLevelStart.None || progress == null || progress.Lives <= 0) return;
            _lives.WaitingStart = WaitingLevelStart.None;
            _signals.Outgoing.LivesOfferFinished.Dispatch();
            if (waiting == WaitingLevelStart.Next) _signals.Incoming.NextLevel.Dispatch();
            else _signals.Incoming.Start.Dispatch();
        }
    }
}
