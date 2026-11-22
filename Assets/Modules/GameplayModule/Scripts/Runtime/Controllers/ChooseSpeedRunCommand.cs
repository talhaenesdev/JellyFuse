using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A Speed Run is started: a try is spent, its levels are drawn at random from those won (above the duel floor),
    /// and its clock is a share of their times added up. With no try left, or too few levels won, nothing starts.
    /// </summary>
    internal sealed class ChooseSpeedRunCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ISpeedRunModel _run { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IDuelModel _duel { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var events = _shared.GetScriptable<CD_Events>();
            var levels = _shared.GetScriptable<CD_JellyLevels>();
            if (progress == null || events == null || levels == null || _duel.Active) { Retain(); Stop(); return; }
            EventsFunction.RefreshDay(progress, events);
            int won = levels.StartIndex(progress);
            if (EventsFunction.SpeedRunTriesLeft(progress) <= 0 || won <= events.DuelLevelFloor + events.SpeedRunLevels)
            {
                FlowLogger.Log("ChooseSpeedRunCommand - no try left, or too few levels won for a run.");
                Retain();
                Stop();
                return;
            }
            var picked = new List<int>();
            for (int guard = 0; picked.Count < events.SpeedRunLevels && guard < 200; guard++)
            {
                int index = levels.RandomPlayableBelow(won, events.DuelLevelFloor);
                if (index >= 0 && !picked.Contains(index)) picked.Add(index);
            }
            if (picked.Count < events.SpeedRunLevels) { Retain(); Stop(); return; }
            float seconds = 0;
            foreach (int index in picked) seconds += levels.Get(index).CreateLevel(levels.Palette).Seconds;
            seconds *= events.SpeedRunTimeShare;
            bool firstTry = progress.SpeedRunPlayed == 0;
            progress.SpeedRunPlayed++;
            _model.Daily = false;
            _run.Begin(picked, seconds, firstTry);
            FlowLogger.Log($"ChooseSpeedRunCommand - {picked.Count} levels on {seconds:0}s.");
        }
    }
}
