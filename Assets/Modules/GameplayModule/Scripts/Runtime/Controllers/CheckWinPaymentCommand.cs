using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// After a move: when it won the level and the win has not been paid yet, asks for the level's
    /// coin reward to be paid, with the weekend and win streak bonuses. A win is paid once, however it is reached.
    /// </summary>
    internal class CheckWinPaymentCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplayInternalSignals _internalSignals { get; set; }

        public override void Execute()
        {
            if (_model.Status != PuzzleStatus.Won || _model.WinPaid) return;
            _model.WinPaid = true;
            var levels = _shared.GetScriptable<CD_JellyLevels>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var events = _shared.GetScriptable<CD_Events>();
            int reward;
            if (_model.Daily && progress != null && events != null)
            {
                // The daily challenge pays its own reward, once a day: its coins as the win's, its booster on top.
                bool first = progress.DailyChallengeDay != ShopDayFunction.Today();
                progress.DailyChallengeDay = ShopDayFunction.Today();
                var daily = events.DailyChallengeReward;
                reward = first && daily != null ? daily.Coins : 0;
                if (first && daily != null && daily.BoosterCount > 0) progress.Booster(daily.Booster).Count += daily.BoosterCount;
            }
            else
            {
                reward = levels != null ? levels.CoinReward(_model.LevelIndex) : 0;
                reward = EventsFunction.WinReward(reward, progress, events);
            }
            _model.WinReward = reward;
            _internalSignals.PayWin.Dispatch(reward);
        }
    }
}
