using System;
using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// The daily events: the weekend coin bonus, the win streak's tiers, the seven login rewards, the
    /// quests a day picks from and the prize wheel. Filed in GameplaySystemRoot's Shared Scriptables.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_Events", menuName = "Jelly Fuse/Events")]
    public sealed class CD_Events : ScriptableObject
    {
        [Header("Weekend bonus")]
        [Tooltip("A win's coins are multiplied by this on the bonus days; 1 turns the bonus off.")]
        [Min(1)] public int WeekendMultiplier = 2;
        [Tooltip("The player's local days the bonus runs on.")]
        public List<DayOfWeek> WeekendDays = new() { DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

        [Header("Win streak")]
        [Tooltip("Wins in a row each tier starts at, lowest first.")]
        public List<int> StreakTierWins = new() { 2, 4, 6 };
        [Tooltip("Extra coins a win pays in each tier, in percent, in the order of Streak Tier Wins.")]
        public List<int> StreakTierBonusPercent = new() { 10, 25, 50 };

        [Header("Login rewards")]
        [Tooltip("One reward a day, taken in order; the last is the big one. Missing a day starts again from the first.")]
        public List<RewardCVO> LoginRewards = new();

        [Header("Daily quests")]
        [Tooltip("How many quests a day gives, picked from Quest Pool with no type twice.")]
        [Min(1)] public int QuestsPerDay = 3;
        public List<QuestCVO> QuestPool = new();

        [Header("Prize wheel")]
        [Tooltip("The wheel's slices clockwise from the top, as many as the wheel's art has; a spin - one free a day, one more for each ad watched - lands on one of them.")]
        public List<WheelSliceCVO> Wheel = new();

        [Header("Slot machine")]
        [Tooltip("The reels' symbols, in the order of the slot screen's symbol art; a spin - one free a day, one more for each ad watched - stops on three of them.")]
        public List<SlotSymbolCVO> SlotSymbols = new();
        [Tooltip("How likely two alike are, against the symbols' weights and the miss's.")]
        [Min(0)] public float SlotPairWeight = 3;
        public RewardCVO SlotPairReward = new() { Coins = 20 };
        [Tooltip("How likely no two alike are, against the symbols' weights and the pair's.")]
        [Min(0)] public float SlotMissWeight = 4;
        public RewardCVO SlotMissReward = new() { Coins = 5 };

        [Header("Scratch card")]
        [Tooltip("The card's prizes, in the order of the scratch screen's prize art; three alike under the nine cells win it.")]
        public List<ScratchPrizeCVO> ScratchPrizes = new();
        [Tooltip("How likely a card wins nothing, against the prizes' weights.")]
        [Min(0)] public float ScratchMissWeight = 3;
        public RewardCVO ScratchMissReward = new() { Coins = 10 };

        [Tooltip("The daily challenge's levels: one a day, in turn, the list starting over when it runs out. Empty: no daily challenge.")]
        public List<CD_JellyLevel> DailyChallengeLevels = new();
        [Tooltip("What winning the daily challenge gives, once a day. Its coins are the win's coins.")]
        public RewardCVO DailyChallengeReward = new() { Coins = 150, Booster = BoosterType.Rocket, BoosterCount = 1 };

        [Tooltip("Coins a duel win pays - beating the opponent's time - for the first DuelRewardedWins wins of a day.")]
        [Min(0)] public int DuelRewardCoins = 20;
        [Min(0)] public int DuelRewardedWins = 3;
        [Tooltip("The fewest levels a player must have won before the duel opens.")]
        [Min(1)] public int DuelMinLevel = 5;
        [Tooltip("Duels never race the first this many levels - too easy to be a race - while the player has won more than that.")]
        [Min(0)] public int DuelLevelFloor = 10;
        [Tooltip("A bot opponent finishes in this share of the level's clock, picked between the two.")]
        [Range(0.1f, 1f)] public float DuelBotMinShare = 0.45f;
        [Range(0.1f, 1f)] public float DuelBotMaxShare = 0.75f;
        [Tooltip("Seconds from the opponent found to GO: what is more than 3 shows the opponent, then 3-2-1.")]
        [Min(0)] public float DuelCountdown = 4.5f;
        [Tooltip("The search shows at least this long, so a quick answer still reads as a search.")]
        [Min(0)] public float DuelMinSearch = 2f;

        public bool IsWeekend(DateTime localDay) => WeekendMultiplier > 1 && WeekendDays.Contains(localDay.DayOfWeek);

        /// <summary>The tier a streak reaches: 0 for none, 1 for the first.</summary>
        public int StreakTier(int wins)
        {
            int tier = 0;
            for (int i = 0; i < StreakTierWins.Count; i++)
                if (wins >= StreakTierWins[i]) tier = i + 1;
            return tier;
        }

        public int StreakBonusPercent(int wins)
        {
            int tier = StreakTier(wins);
            return tier > 0 && tier <= StreakTierBonusPercent.Count ? StreakTierBonusPercent[tier - 1] : 0;
        }
    }
}
