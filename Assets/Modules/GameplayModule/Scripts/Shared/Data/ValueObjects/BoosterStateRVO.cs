using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>A booster as the HUD shows it right now.</summary>
    public sealed class BoosterStateRVO
    {
        public BoosterType Type;
        public Sprite Icon;
        public bool Unlocked;
        public int UnlockLevel;
        public int Count;
        public int Price;
        /// <summary>Usable now: a use left, or none and the button opens the offer for more.</summary>
        public bool Usable;
        /// <summary>No use left, and today's free booster is offered for an ad: pressing it shows the ad.</summary>
        public bool FreeByAd;
        /// <summary>No use left: pressing it opens the purchase popup on this booster.</summary>
        public bool OnOffer;
    }
}
