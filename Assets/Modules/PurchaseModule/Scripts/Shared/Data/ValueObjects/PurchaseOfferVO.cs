using UnityEngine;

namespace Modules.PurchaseModule.Shared.Data.ValueObjects
{
    /// <summary>
    /// One offer as the purchase popup shows it. The module that sells makes it; the popup only
    /// draws it and reports a press of Buy under <see cref="Key"/>. Texts are localization keys,
    /// each with the English shown when the key has no text.
    /// </summary>
    public sealed class PurchaseOfferVO
    {
        /// <summary>Names the offer: the popup's Buy and refresh are announced under it.</summary>
        public string Key;
        public string TitleKey, Title;
        public Sprite Icon;
        /// <summary>What the player holds now, drawn over the icon: "4/5", "120".</summary>
        public string Amount;
        /// <summary>The line under the icon; a {0} in it is the countdown.</summary>
        public string InfoKey, Info;
        /// <summary>Above 0: the popup counts it down in the info line, and asks for the offer again at zero.</summary>
        public float CountdownSeconds;
        /// <summary>The buy button's text; a {0} in it is the price.</summary>
        public string BuyKey, Buy;
        public int Price;
        public bool CanBuy;
    }
}
