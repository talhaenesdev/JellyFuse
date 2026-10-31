using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
namespace Modules.GameplayModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// The player's progress, kept between runs: filed on LocalSaveServiceRoot, it is read back
    /// before any module starts and written whenever the level being played changes. The values
    /// here are what a new player starts with.
    /// </summary>
    [CreateAssetMenu(fileName = "SD_JellyProgress", menuName = "Jelly Fuse/Progress")]
    public sealed class SD_JellyProgress : ScriptableObject
    {
        [Tooltip("Position in CD_JellyLevels of the level the player is on; -1 starts from CD_JellyLevels' start level.")]
        public int LevelIndex = -1;
        [Tooltip("The coins the player holds. Levels add to it when they are won.")]
        [Min(0)] public int Coins;
        [Tooltip("The player's boosters: uses left, and whether the free uses were given.")]
        public List<BoosterSVO> Boosters = new();
        [Tooltip("The lives the player holds.")]
        [Min(0)] public int Lives;
        [Tooltip("Whether the player was given CD_Lives' start lives.")]
        public bool LivesGranted;
        [Tooltip("When the next life comes back, in Unix seconds; 0 while lives are full.")]
        public long NextLifeAt;
        [Tooltip("Whether the player bought the removal of ads. Bought once, kept for ever.")]
        public bool NoAds;
        /// <summary>The day each once-a-day product was last bought.</summary>
        public List<ShopPurchaseDaySVO> ShopPurchaseDays = new();

        /// <summary>The player's entry for a booster, made when it has none yet.</summary>
        public BoosterSVO Booster(BoosterType type)
        {
            var entry = Boosters.Find(b => b != null && b.Type == type);
            if (entry == null) Boosters.Add(entry = new BoosterSVO { Type = type });
            return entry;
        }
    }
}
