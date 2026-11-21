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
    /// Keeps the live duel's messages coming: listens once the player is signed in, each message handed on as
    /// LiveMessage, and listens again whenever the connection was lost - the app went to the background, the network
    /// changed. One turn of a loop that checks again a little later.
    /// </summary>
    [LongRetain(70)]
    internal class ListenLiveDuelCommand : Command
    {
        private const int CHECK_MS = 15000;

        [Inject] private IDuelService _duelService { get; set; }
        [InjectSignal] private GameplayInternalSignals _internalSignals { get; set; }

        public override async void Execute()
        {
            Retain();
            var service = _duelService;
            var signals = _internalSignals;
            try
            {
                if (service.IsReady && !service.IsListening)
                {
                    bool listening = await service.ListenAsync((type, body) => signals.LiveMessage.Dispatch(type, body));
                    FlowLogger.Log($"ListenLiveDuelCommand - listening for live duels: {listening}");
                }
                // Once: how far this phone's clock is off, so a live race starts at the server's moment on both games.
                if (service.IsReady && !service.ClockSynced) await service.SyncClockAsync();
                await Task.Delay(CHECK_MS);
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
