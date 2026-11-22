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
using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Attributes;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A Speed Run level was won with more to come: the time left carries over and, once the last jelly has been seen
    /// going in, the next level starts.
    /// </summary>
    [LongRetain(10)]
    internal sealed class AdvanceSpeedRunCommand : Command
    {
        private const int PAUSE_MS = 1200;

        [Inject] private ISpeedRunModel _run { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override async void Execute()
        {
            if (!_run.Active || _run.IsLast || _model.Status != PuzzleStatus.Won) return;
            Retain();
            var run = _run;
            var signals = _signals;
            try
            {
                run.Advance(_model.RemainingSeconds);
                await Task.Delay(PAUSE_MS);
                Release();
                if (run.Active) signals.Incoming.Start.Dispatch();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"AdvanceSpeedRunCommand threw while moving on: {exception}");
                Stop();
            }
        }
    }
}
