using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// Lives: what a new player gets, how they come back over time, and what one costs. A failed
    /// level takes a life; a level cannot be started without one. Filed in GameplaySystemRoot's
    /// Shared Scriptables.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_Lives", menuName = "Jelly Fuse/Lives")]
    public sealed class CD_Lives : ScriptableObject
    {
        [Tooltip("Lives a new player starts with.")]
        [Min(0)] public int StartLives = 5;
        [Tooltip("The most lives the player can hold: refills stop here, and lives cannot be bought past it.")]
        [Min(1)] public int MaxLives = 5;
        [Tooltip("Minutes between two refills while the player holds fewer than Max Lives.")]
        [Min(.05f)] public float RefillMinutes = 30;
        [Tooltip("Lives each refill gives.")]
        [Min(1)] public int LivesPerRefill = 1;
        [Tooltip("On: with no life left, starting a level buys lives with coins when the player has enough.")]
        public bool BuyWithCoins = true;
        [Tooltip("Coins one purchase costs.")]
        [Min(0)] public int CoinPrice = 100;
        [Tooltip("Lives one purchase gives.")]
        [Min(1)] public int LivesPerPurchase = 1;

        [Header("Purchase popup")]
        [Tooltip("The picture on the popup; the lives held are written over it.")]
        public Sprite OfferIcon;
        [Tooltip("Localization key of the popup's title.")]
        public string TitleKey = "lives.more_lives";
        [Tooltip("Localization key of the line under the icon while a life is on its way; {0} is the time left.")]
        public string NextLifeKey = "lives.next_life";
        [Tooltip("Localization key of the line under the icon while lives are full.")]
        public string FullKey = "lives.full";
        [Tooltip("Localization key of the buy button; {0} is the price.")]
        public string BuyKey = "lives.buy";

        public long RefillSeconds => System.Math.Max(1, (long)Mathf.Round(RefillMinutes * 60));
    }
}
