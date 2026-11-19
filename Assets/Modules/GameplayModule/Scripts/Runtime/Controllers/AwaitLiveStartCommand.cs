using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.DuelModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A live invite was accepted: the yes goes to the server and the start push is waited on. A yes refused - the
    /// invite lapsed, the sender left - or a start that never comes tells the screen the duel is off, so the player
    /// gets the way back to the menu instead of a panel that waits for ever.
    /// </summary>
    [LongRetain(40)]
    internal class AwaitLiveStartCommand : Command
    {
        private const int START_WAIT_SECONDS = 12;

        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private IDuelService _duelService { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private string _matchId { get; set; }

        public override async void Execute()
        {
            Retain();
            var duel = _duel;
            var signals = _signals;
            try
            {
                bool answered = await _duelService.AnswerLiveAsync(_matchId, true);
                for (int i = 0; answered && i < START_WAIT_SECONDS * 2 && !duel.Live; i++) await Task.Delay(500);
                if (!duel.Live)
                {
                    FlowLogger.Log($"AwaitLiveStartCommand - live match {_matchId} did not start (answered: {answered}).");
                    signals.Outgoing.DuelUnavailable.Dispatch();
                }
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"AwaitLiveStartCommand threw while waiting for the start: {exception}");
                signals.Outgoing.DuelUnavailable.Dispatch();
                Stop();
            }
        }
    }
}
