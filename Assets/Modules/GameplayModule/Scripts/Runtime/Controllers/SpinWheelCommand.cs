using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Spins the prize wheel with today's free spin, or with a spin an ad gave once that is taken, and gives what it
    /// lands on. No spin held, and the sequence stops.
    /// </summary>
    internal class SpinWheelCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var config = _shared.GetScriptable<CD_Events>();
            if (EventsFunction.WheelSpins(progress, config) <= 0)
            {
                Retain();
                Stop();
                return;
            }
            if (!progress.WheelFreeTaken) progress.WheelFreeTaken = true;
            else progress.WheelSpins--;
            int slice = EventsFunction.SpinWheel(progress, config);
            _signals.Outgoing.WheelSpun.Dispatch(slice);
            _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
        }
    }
}
