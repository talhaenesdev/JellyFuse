using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Enums;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A level is played only with a life in hand. With none, the level does not start: the player
    /// is told when the next life comes back and, where CD_Lives sells lives for coins, offered them.
    /// Coins are spent only when the player buys from that offer, never on their behalf; the start
    /// held back is noted, so buying a life there plays it.
    /// </summary>
    internal class RequireLifeCommand : Command<WaitingLevelStart>
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILivesModel _lives { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute(WaitingLevelStart start)
        {
            var config = _shared.GetScriptable<CD_Lives>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (config == null || progress == null || progress.Lives > 0)
            {
                _lives.WaitingStart = WaitingLevelStart.None;
                return;
            }
            _lives.WaitingStart = start;
            var state = _lives.State(progress, config, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            _signals.Outgoing.LivesChanged.Dispatch(state);
            _signals.Outgoing.OutOfLives.Dispatch(state.SecondsToNext);
            if (config.BuyWithCoins) _signals.Outgoing.LivesOfferOpened.Dispatch(LivesOfferFunction.Build(_lives, progress, config));
            Retain();
            Stop();
        }
    }
}
