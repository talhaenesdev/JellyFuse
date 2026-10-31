using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Lets the free booster ad through only on an open booster with no use left, once a day.</summary>
    internal class CheckBoosterAdCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [SignalParam] private BoosterType _type { get; set; }

        public override void Execute()
        {
            if (BoosterAdFunction.Offered(_shared, _model, _type)) return;
            Retain();
            Stop();
        }
    }
}
