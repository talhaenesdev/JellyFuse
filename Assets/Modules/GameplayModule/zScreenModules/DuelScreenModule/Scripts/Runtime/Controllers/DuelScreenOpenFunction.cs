using System.Threading.Tasks;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.DuelScreenModule.ViewsMediators;
using Modules.LocalizationModule.Services;
using Modules.ProfileModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.DuelScreenModule.Controllers
{
    /// <summary>The duel screen already up, or opened and given its texts and the player's profile; null when it did not open.</summary>
    internal static class DuelScreenOpenFunction
    {
        public static async Task<DuelScreenView> Get(IScreenService screens, ISharedDataModel shared, ILocalizationService localization)
        {
            if (screens.Check.IsScreenActive<DuelScreenView>() && screens.TryGet.Screen(out DuelScreenView open) && open != null)
                return open;
            DuelScreenView screen = await screens.Open<DuelScreenView>().Show<DuelScreenView>();
            if (screen == null) return null;
            screen.SetLocalizer(localization.Get);
            var profile = shared.GetScriptable<SD_PlayerProfile>();
            screen.SetProfile(shared.GetScriptable<CD_ProfileIcons>(), profile?.PlayerName, profile?.SelectedIconId);
            return screen;
        }
    }
}
