using System;
using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>The daily events' bookkeeping on the player's save: a new day's quests, the login run, a reward given, and how it all stands.</summary>
    internal static class EventsFunction
    {
        /// <summary>A new day picks new quests, and a login run with a day missed - or all its days taken - starts again.</summary>
        public static void RefreshDay(SD_JellyProgress progress, CD_Events config)
        {
            int today = ShopDayFunction.Today();
            if (progress.QuestDay != today)
            {
                progress.QuestDay = today;
                progress.Quests = PickQuests(config);
            }
            bool missedDay = progress.LoginRewardDay >= 0 && progress.LoginRewardDay < today - 1;
            bool runDone = progress.LoginRewardsTaken >= config.LoginRewards.Count && progress.LoginRewardDay < today;
            if (missedDay || runDone) progress.LoginRewardsTaken = 0;
            if (progress.WheelDay != today)
            {
                progress.WheelDay = today;
                progress.WheelFreeTaken = false;
            }
        }

        /// <summary>Spins of the prize wheel the player can take now: today's free one while it is left, and those ads gave.</summary>
        public static int WheelSpins(SD_JellyProgress progress, CD_Events config) =>
            progress == null || config == null || config.Wheel.Count == 0 ? 0 : (progress.WheelFreeTaken ? 0 : 1) + progress.WheelSpins;

        /// <summary>Picks a slice by the slices' weights, gives its reward, and returns where it is on the wheel.</summary>
        public static int SpinWheel(SD_JellyProgress progress, CD_Events config)
        {
            float total = 0;
            foreach (var slice in config.Wheel) total += slice.Weight;
            float roll = UnityEngine.Random.value * total;
            int picked = config.Wheel.Count - 1;
            for (int i = 0; i < config.Wheel.Count; i++)
            {
                roll -= config.Wheel[i].Weight;
                if (roll < 0 && config.Wheel[i].Weight > 0) { picked = i; break; }
            }
            Grant(progress, config.Wheel[picked].Reward);
            return picked;
        }

        public static bool LoginClaimable(SD_JellyProgress progress, CD_Events config) =>
            progress != null && config != null &&
            progress.LoginRewardDay != ShopDayFunction.Today() && progress.LoginRewardsTaken < config.LoginRewards.Count;

        public static void Advance(SD_JellyProgress progress, QuestType type, int amount)
        {
            if (amount <= 0 || progress.Quests == null) return;
            foreach (var quest in progress.Quests)
                if (quest.Type == type && !quest.Claimed) quest.Progress = Mathf.Min(quest.Target, quest.Progress + amount);
        }

        public static void Grant(SD_JellyProgress progress, RewardCVO reward)
        {
            if (reward == null) return;
            progress.Coins += reward.Coins;
            if (reward.BoosterCount > 0) progress.Booster(reward.Booster).Count += reward.BoosterCount;
        }

        public static EventsRVO Snapshot(SD_JellyProgress progress, CD_Events config)
        {
            bool claimable = LoginClaimable(progress, config);
            var days = new List<LoginDayRVO>();
            for (int i = 0; i < config.LoginRewards.Count; i++)
            {
                var state = i < progress.LoginRewardsTaken ? LoginDayState.Claimed
                    : i == progress.LoginRewardsTaken && claimable ? LoginDayState.Today
                    : LoginDayState.Locked;
                days.Add(new LoginDayRVO { Reward = config.LoginRewards[i], State = state });
            }
            var quests = progress.Quests ?? new List<QuestSVO>();
            return new EventsRVO
            {
                WeekendMultiplier = config.IsWeekend(DateTime.Now) ? config.WeekendMultiplier : 1,
                WinStreak = progress.WinStreak,
                WinStreakTier = config.StreakTier(progress.WinStreak),
                WinStreakBonusPercent = config.StreakBonusPercent(progress.WinStreak),
                LoginDays = days,
                LoginClaimable = claimable,
                Quests = quests,
                QuestClaimable = quests.Exists(q => q.IsClaimable),
                QuestsResetAt = new DateTimeOffset(DateTime.Now.Date.AddDays(1)).ToUnixTimeSeconds(),
                Wheel = config.Wheel,
                WheelSpins = WheelSpins(progress, config),
            };
        }

        /// <summary>A win's coins after the weekend bonus and the win streak's bonus.</summary>
        public static int WinReward(int coins, SD_JellyProgress progress, CD_Events config)
        {
            if (config == null || progress == null) return coins;
            int multiplier = config.IsWeekend(DateTime.Now) ? config.WeekendMultiplier : 1;
            int bonus = config.StreakBonusPercent(progress.WinStreak);
            return Mathf.RoundToInt(coins * multiplier * (1 + bonus / 100f));
        }

        private static List<QuestSVO> PickQuests(CD_Events config)
        {
            var pool = new List<QuestCVO>(config.QuestPool);
            var picked = new List<QuestSVO>();
            var used = new HashSet<QuestType>();
            while (picked.Count < config.QuestsPerDay && pool.Count > 0)
            {
                var quest = pool[UnityEngine.Random.Range(0, pool.Count)];
                pool.Remove(quest);
                if (!used.Add(quest.Type)) continue;
                picked.Add(new QuestSVO
                {
                    Type = quest.Type,
                    Target = quest.Target,
                    Reward = new RewardCVO { Coins = quest.Reward.Coins, Booster = quest.Reward.Booster, BoosterCount = quest.Reward.BoosterCount },
                });
            }
            return picked;
        }
    }
}
