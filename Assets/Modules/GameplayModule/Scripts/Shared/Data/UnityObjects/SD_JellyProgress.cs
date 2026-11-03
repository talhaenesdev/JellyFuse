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
        /// <summary>The local day today's free booster was taken for an ad (days since 2000-01-01); -1 never.</summary>
        public int FreeBoosterAdDay = -1;
        [Tooltip("Levels won in a row; a level lost or left starts it again.")]
        [Min(0)] public int WinStreak;
        [Tooltip("Login rewards taken in the current run of days, 0 to CD_Events' count.")]
        [Min(0)] public int LoginRewardsTaken;
        /// <summary>The local day the last login reward was taken (days since 2000-01-01); -1 never.</summary>
        public int LoginRewardDay = -1;
        /// <summary>The local day Quests were picked for; -1 never.</summary>
        public int QuestDay = -1;
        public List<QuestSVO> Quests = new();
        /// <summary>The local day the wheel's spins below count for; -1 never.</summary>
        public int WheelDay = -1;
        [Tooltip("Whether today's free spin of the prize wheel is taken.")]
        public bool WheelFreeTaken;
        [Tooltip("Spins of the prize wheel ads gave and the player has not taken yet.")]
        [Min(0)] public int WheelSpins;

        /// <summary>The player's entry for a booster, made when it has none yet.</summary>
        public BoosterSVO Booster(BoosterType type)
        {
            var entry = Boosters.Find(b => b != null && b.Type == type);
            if (entry == null) Boosters.Add(entry = new BoosterSVO { Type = type });
            return entry;
        }
    }
}
