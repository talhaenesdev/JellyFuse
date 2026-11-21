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
    /// <summary>Holds the flow while the charged jellies crackle and rise; a level that ended meanwhile ends it.</summary>
    [LongRetain(15)]
    internal sealed class WaitComboStrikeCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        public override async void Execute()
        {
            Retain();
            var model = _model;
            try
            {
                var config = _shared.GetScriptable<CD_Boosters>();
                await Task.Delay(TimeSpan.FromSeconds(config != null ? config.ComboStrikeSeconds : 1f));
                if (model.Snapshot().Status != PuzzleStatus.Playing) { Stop(); return; }
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"WaitComboStrikeCommand threw while waiting: {exception}");
                Stop();
            }
        }
    }
}
