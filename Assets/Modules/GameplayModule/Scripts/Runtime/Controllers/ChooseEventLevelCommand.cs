using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>An event card's Play: a level the player has won is picked for that event. Too few won, and nothing starts.</summary>
    internal sealed class ChooseEventLevelCommand : Command<EventPlayKind>
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IEventPlayModel _event { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IDuelModel _duel { get; set; }

        public override void Execute(EventPlayKind kind)
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var events = _shared.GetScriptable<CD_Events>();
            if (progress != null && events != null) EventsFunction.RefreshDay(progress, events);
            int index = _duel.Active ? -1 : EventLevelFunction.Pick(kind, _shared.GetScriptable<CD_JellyLevels>(), progress, events, _event.LevelIndex);
            if (index < 0)
            {
                FlowLogger.Log("ChooseEventLevelCommand - too few levels won to play an event level.");
                Retain();
                Stop();
                return;
            }
            _model.Daily = false;
            _event.Begin(kind, index);
            FlowLogger.Log($"ChooseEventLevelCommand - {kind} plays level {index + 1}.");
        }
    }
}
