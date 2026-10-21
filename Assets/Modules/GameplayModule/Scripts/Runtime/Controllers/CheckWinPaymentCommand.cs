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
    /// coin reward to be paid. A win is paid once, however it is reached.
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
            int reward = levels != null ? levels.CoinReward(_model.LevelIndex) : 0;
            _internalSignals.PayWin.Dispatch(reward);
        }
    }
}
