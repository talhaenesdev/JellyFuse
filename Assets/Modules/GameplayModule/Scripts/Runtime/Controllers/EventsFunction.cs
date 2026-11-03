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
                progress.SlotFreeTaken = false;
                progress.ScratchFreeTaken = false;
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

        /// <summary>Spins of the slot machine the player can take now: today's free one while it is left, and those ads gave.</summary>
        public static int SlotSpins(SD_JellyProgress progress, CD_Events config) =>
            progress == null || config == null || config.SlotSymbols.Count < 2 ? 0 : (progress.SlotFreeTaken ? 0 : 1) + progress.SlotSpins;

        /// <summary>Scratch cards the player can take now: today's free one while it is left, and those ads gave.</summary>
        public static int ScratchCards(SD_JellyProgress progress, CD_Events config) =>
            progress == null || config == null || config.ScratchPrizes.Count < 2 ? 0 : (progress.ScratchFreeTaken ? 0 : 1) + progress.ScratchCards;

        /// <summary>
        /// Rolls the slot machine's outcome by the weights - three of a symbol, a pair, or a miss - lays reels that show
        /// it, and gives what it pays.
        /// </summary>
        public static SlotResultRVO SpinSlot(SD_JellyProgress progress, CD_Events config)
        {
            var symbols = config.SlotSymbols;
            int count = symbols.Count;
            float total = config.SlotPairWeight + config.SlotMissWeight;
            foreach (var symbol in symbols) total += symbol.Weight;
            float roll = UnityEngine.Random.value * total;
            var result = new SlotResultRVO();
            int triple = -1;
            for (int i = 0; i < count; i++)
            {
                roll -= symbols[i].Weight;
                if (roll < 0 && symbols[i].Weight > 0) { triple = i; break; }
            }
            if (triple >= 0)
            {
                result.Reels = new[] { triple, triple, triple };
                result.Reward = symbols[triple].Reward;
                result.Jackpot = true;
            }
            else if (roll - config.SlotPairWeight < 0 && config.SlotPairWeight > 0)
            {
                int pair = UnityEngine.Random.Range(0, count);
                int odd = (pair + UnityEngine.Random.Range(1, count)) % count;
                int oddReel = UnityEngine.Random.Range(0, 3);
                for (int i = 0; i < 3; i++) result.Reels[i] = i == oddReel ? odd : pair;
                result.Reward = config.SlotPairReward;
            }
            else
            {
                // No two alike: three different symbols, which needs at least three; two symbols can only pair.
                var pool = new List<int>();
                for (int i = 0; i < count; i++) pool.Add(i);
                for (int i = 0; i < 3; i++)
                {
                    if (pool.Count == 0) { result.Reels[i] = result.Reels[0]; continue; }
                    int at = UnityEngine.Random.Range(0, pool.Count);
                    result.Reels[i] = pool[at];
                    pool.RemoveAt(at);
                }
                result.Reward = config.SlotMissReward;
            }
            Grant(progress, result.Reward);
            return result;
        }

        /// <summary>
        /// Picks a scratch card's prize by the weights - or none - lays nine cells that show three of it and no other
        /// three alike, and gives what it pays.
        /// </summary>
        public static ScratchCardRVO DealScratch(SD_JellyProgress progress, CD_Events config)
        {
            var prizes = config.ScratchPrizes;
            float total = config.ScratchMissWeight;
            foreach (var prize in prizes) total += prize.Weight;
            float roll = UnityEngine.Random.value * total;
            int won = -1;
            for (int i = 0; i < prizes.Count; i++)
            {
                roll -= prizes[i].Weight;
                if (roll < 0 && prizes[i].Weight > 0) { won = i; break; }
            }
            var cells = new List<int>();
            if (won >= 0) { cells.Add(won); cells.Add(won); cells.Add(won); }
            // The other cells: any prize but the one won, at most twice each, and empty cells once those run out.
            var others = new List<int>();
            for (int i = 0; i < prizes.Count; i++)
                if (i != won) { others.Add(i); others.Add(i); }
            while (cells.Count < 9)
            {
                if (others.Count == 0) { cells.Add(-1); continue; }
                int at = UnityEngine.Random.Range(0, others.Count);
                cells.Add(others[at]);
                others.RemoveAt(at);
            }
            for (int i = cells.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }
            var card = new ScratchCardRVO
            {
                Cells = cells.ToArray(),
                WonPrize = won,
                Reward = won >= 0 ? prizes[won].Reward : config.ScratchMissReward,
            };
            Grant(progress, card.Reward);
            return card;
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
                SlotSpins = SlotSpins(progress, config),
                ScratchCards = ScratchCards(progress, config),
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
