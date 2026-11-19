using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DuelModule.Services;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Starts listening for the live duel's messages once the player is signed in, each one handed on as LiveMessage.
    /// Not signed in after a minute, it gives up until the next launch: live invites then simply do not arrive.
    /// </summary>
    [LongRetain(70)]
    internal class ListenLiveDuelCommand : Command
    {
        [Inject] private IDuelService _duelService { get; set; }
        [InjectSignal] private GameplayInternalSignals _internalSignals { get; set; }

        public override async void Execute()
        {
            Retain();
            var service = _duelService;
            var signals = _internalSignals;
            try
            {
                for (int i = 0; i < 30 && !service.IsReady; i++) await Task.Delay(2000);
                bool listening = await service.ListenAsync((type, body) => signals.LiveMessage.Dispatch(type, body));
                FlowLogger.Log($"ListenLiveDuelCommand - listening for live duels: {listening}");
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"ListenLiveDuelCommand - not listening for live duels: {exception.Message}");
                Release();
            }
        }
    }
}
