using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Takes today's free spin of the prize wheel and gives what it lands on. Taken already, and the sequence stops.</summary>
    internal class SpinWheelCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var config = _shared.GetScriptable<CD_Events>();
            if (!EventsFunction.WheelFreeSpin(progress, config))
            {
                Retain();
                Stop();
                return;
            }
            progress.WheelFreeTaken = true;
            int slice = EventsFunction.SpinWheel(progress, config);
            _signals.Outgoing.WheelSpun.Dispatch(slice);
            _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
        }
    }
}
