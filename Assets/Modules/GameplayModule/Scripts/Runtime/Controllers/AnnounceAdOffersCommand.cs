using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.AdsModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Tells the result panels which rewarded ads they offer: the win's coins again once a win is paid, more time
    /// once the clock ran out. Each once a level, and only while an ad is loaded to show.
    /// </summary>
    internal class AnnounceAdOffersCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IAdsService _ads { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_AdRewards>();
            if (config == null || !_ads.IsRewardedReady)
            {
                _signals.Outgoing.AdOffersChanged.Dispatch(AdOffersRVO.None);
                return;
            }
            _signals.Outgoing.AdOffersChanged.Dispatch(new AdOffersRVO
            {
                DoubleReward = _model.Status == PuzzleStatus.Won && _model.WinPaid && !_model.RewardDoubled
                    && _model.WinReward > 0 && config.WinRewardMultiplier > 1,
                Multiplier = config.WinRewardMultiplier,
                DoubleRewardKey = config.DoubleRewardKey,
                ExtraTime = _model.Status == PuzzleStatus.Lost && _model.RemainingSeconds <= 0 && !_model.TimeExtended
                    && config.ExtraSeconds > 0,
                ExtraSeconds = Mathf.RoundToInt(config.ExtraSeconds),
                ExtraTimeKey = config.ExtraTimeKey,
            });
        }
    }
}
