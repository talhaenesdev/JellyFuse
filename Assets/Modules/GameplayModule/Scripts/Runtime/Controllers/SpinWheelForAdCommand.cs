using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The ad was watched: one of today's ad spins is used and the prize wheel gives what it lands on.</summary>
    internal class SpinWheelForAdCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            progress.WheelAdSpins++;
            int slice = EventsFunction.SpinWheel(progress, _shared.GetScriptable<CD_Events>());
            _signals.Outgoing.WheelSpun.Dispatch(slice);
            _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
        }
    }
}
