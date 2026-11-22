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
            if (progress.SpeedRunDay != today)
            {
                progress.SpeedRunDay = today;
                progress.SpeedRunPlayed = 0;
                progress.SpeedRunAdGranted = 0;
                progress.SpeedRunWon = false;
            }
            if (progress.CarnivalDay != today)
            {
                progress.CarnivalDay = today;
                progress.CarnivalPoints = 0;
                progress.CarnivalOpened = 0;
                var colours = config.CarnivalColours;
                progress.CarnivalColour = colours != null && colours.Count > 0
                    ? (int)colours[UnityEngine.Random.Range(0, colours.Count)] : (int)JellyColour.Blue;
            }
            int week = StarRushWeek(today);
            if (progress.StarRushWeek != week)
            {
                progress.StarRushWeek = week;
                progress.StarRushStars = 0;
                progress.StarRushClaimed = 0;
            }
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

        /// <summary>Today's daily challenge level: the list's levels one a day in turn, broken ones passed over; null when there is none.</summary>
        public static CD_JellyLevel DailyChallengeLevel(CD_Events config)
        {
            if (config == null || config.DailyChallengeLevels.Count == 0) return null;
            int count = config.DailyChallengeLevels.Count, today = ShopDayFunction.Today();
            for (int i = 0; i < count; i++)
            {
                var level = config.DailyChallengeLevels[((today + i) % count + count) % count];
                if (level != null && level.Validate().Count == 0) return level;
            }
            return null;
        }
        /// <summary>Whether today's daily challenge is there to play: there is a level for today and it is not won yet.</summary>
        public static bool DailyChallengeReady(SD_JellyProgress progress, CD_Events config) =>
            progress.DailyChallengeDay != ShopDayFunction.Today() && DailyChallengeLevel(config) != null;

        /// <summary>Speed Runs the player may start now: the day's free one and those ads gave, less those played.</summary>
        public static int SpeedRunTriesLeft(SD_JellyProgress progress) => Mathf.Max(0, 1 + progress.SpeedRunAdGranted - progress.SpeedRunPlayed);

        /// <summary>Cells of today's colour taken through doors go on the carnival's count.</summary>
        public static void AddCarnival(SD_JellyProgress progress, CD_Events config, int cells)
        {
            if (progress == null || config == null || cells <= 0) return;
            RefreshDay(progress, config);
            progress.CarnivalPoints += cells;
        }

        /// <summary>The next chest that is reached and not yet opened; null when none waits.</summary>
        public static CarnivalChestCVO CarnivalReady(SD_JellyProgress progress, CD_Events config)
        {
            if (progress == null || config == null || progress.CarnivalOpened >= config.CarnivalChests.Count) return null;
            var next = config.CarnivalChests[progress.CarnivalOpened];
            return next != null && progress.CarnivalPoints >= next.Points ? next : null;
        }

        /// <summary>Draws one of a chest's prizes by weight; coins fall anywhere between the prize's least and most.</summary>
        public static CarnivalPrizeRVO DrawCarnivalPrize(CarnivalChestCVO chest, int index)
        {
            var result = new CarnivalPrizeRVO { Chest = index };
            if (chest == null || chest.Prizes == null || chest.Prizes.Count == 0) return result;
            int total = 0;
            foreach (var prize in chest.Prizes) total += Mathf.Max(1, prize.Weight);
            int pick = UnityEngine.Random.Range(0, total);
            CarnivalPrizeCVO drawn = chest.Prizes[chest.Prizes.Count - 1];
            foreach (var prize in chest.Prizes)
            {
                pick -= Mathf.Max(1, prize.Weight);
                if (pick < 0) { drawn = prize; break; }
            }
            result.Coins = drawn.CoinsMax > drawn.CoinsMin ? UnityEngine.Random.Range(drawn.CoinsMin, drawn.CoinsMax + 1) : drawn.CoinsMin;
            result.Rockets = drawn.Rockets; result.Freezes = drawn.Freezes; result.Ufos = drawn.Ufos; result.Lives = drawn.Lives;
            return result;
        }

        /// <summary>The Monday-based week a day falls in: day 0 (2000-01-01) was a Saturday, so Mondays start each count.</summary>
        public static int StarRushWeek(int day) => (day + 5) / 7;

        /// <summary>A normal level's stars go on this week's meter.</summary>
        public static void AddStarRush(SD_JellyProgress progress, CD_Events config, int stars)
        {
            if (progress == null || config == null || stars <= 0) return;
            RefreshDay(progress, config);
            progress.StarRushStars += stars;
        }

        /// <summary>The next step that is reached and not yet paid; null when none waits.</summary>
        public static StarRushMilestoneCVO StarRushReady(SD_JellyProgress progress, CD_Events config)
        {
            if (progress == null || config == null || progress.StarRushClaimed >= config.StarRushMilestones.Count) return null;
            var next = config.StarRushMilestones[progress.StarRushClaimed];
            return next != null && progress.StarRushStars >= next.Stars ? next : null;
        }

        /// <summary>Monday 00:00 of the next week, local time, in unix seconds.</summary>
        public static long StarRushEndsAt()
        {
            var today = DateTime.Now.Date;
            int toMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
            return new DateTimeOffset(today.AddDays(toMonday == 0 ? 7 : toMonday)).ToUnixTimeSeconds();
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
                DailyChallengeReady = DailyChallengeReady(progress, config),
                DailyChallengeReward = config.DailyChallengeReward,
                SpeedRunTriesLeft = SpeedRunTriesLeft(progress),
                SpeedRunAdLeft = Mathf.Max(0, config.SpeedRunAdTries - progress.SpeedRunAdGranted),
                SpeedRunWon = progress.SpeedRunWon,
                SpeedRunLevels = config.SpeedRunLevels,
                SpeedRunCoins = config.SpeedRunCoins,
                SpeedRunRockets = config.SpeedRunRockets,
                SpeedRunFreezes = config.SpeedRunFreezes,
                CarnivalColour = (JellyColour)progress.CarnivalColour,
                CarnivalPoints = progress.CarnivalPoints,
                CarnivalOpened = progress.CarnivalOpened,
                CarnivalChests = config.CarnivalChests,
                CarnivalClaimable = CarnivalReady(progress, config) != null,
                CarnivalEndsAt = new DateTimeOffset(DateTime.Now.Date.AddDays(1)).ToUnixTimeSeconds(),
                StarRushStars = progress.StarRushStars,
                StarRushClaimed = progress.StarRushClaimed,
                StarRushMilestones = config.StarRushMilestones,
                StarRushClaimable = StarRushReady(progress, config) != null,
                StarRushEndsAt = StarRushEndsAt(),
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
