using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Spins the slot machine with today's free spin, or with a spin an ad gave once that is taken, and gives what the
    /// reels show. No spin held, and the sequence stops.
    /// </summary>
    internal class SpinSlotCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var config = _shared.GetScriptable<CD_Events>();
            if (EventsFunction.SlotSpins(progress, config) <= 0)
            {
                Retain();
                Stop();
                return;
            }
            if (!progress.SlotFreeTaken) progress.SlotFreeTaken = true;
            else progress.SlotSpins--;
            var result = EventsFunction.SpinSlot(progress, config);
            _signals.Outgoing.SlotSpun.Dispatch(result);
            _signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
        }
    }
}
