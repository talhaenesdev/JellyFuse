using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Whether a booster is offered for an ad: open at this level, no use left, CD_AdRewards gives some, and today's
    /// free booster not yet taken. Whether an ad is loaded is the caller's to add.
    /// </summary>
    internal static class BoosterAdFunction
    {
        public static bool Offered(ISharedDataModel shared, IJellyPuzzleModel model, BoosterType type)
        {
            var config = shared.GetScriptable<CD_AdRewards>();
            var booster = shared.GetScriptable<CD_Boosters>()?.Get(type);
            var progress = shared.GetScriptable<SD_JellyProgress>();
            return config != null && config.DailyFreeBoosters > 0 && booster != null && booster.Enabled && progress != null
                && model.Status == PuzzleStatus.Playing && model.LevelIndex + 1 >= booster.UnlockLevel
                && progress.Booster(type).Count <= 0 && progress.FreeBoosterAdDay != ShopDayFunction.Today();
        }
    }
}
